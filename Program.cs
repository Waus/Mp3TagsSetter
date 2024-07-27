using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Mp3TagsSetter
{
    static class Program
    {
        // Regex to remove "(Demo)" and "(EP)" from folder names
        static Regex folderNameCleanupRegex = new Regex(@"\s*\((Demo|EP)\)$");
        // Regex to remove "Compilation", "Single", "Live", "Split" from album tag and picture name
        static Regex folderNameKeepTagsRegex = new Regex(@"\s*\((Compilation|Single|Live|Split|2CD|3CD|4CD|5CD)\)$");
        // Regex to check if the directory name is an album name
        static Regex albumDirectoryNameRegex = new Regex(@"^\d{4}\s\-\s[\w* [(\.]+"); // "2020 - Tytuł"

        [STAThread]
        static void Main()
        {
            CleanFolderNames();
            SetFolderNames();
            SetMp3Tags();
        }

        private static void CleanFolderNames()
        {
            string path = Directory.GetCurrentDirectory();
            var allDirectories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

            foreach (var dir in allDirectories)
            {
                try
                {
                    var dirName = Path.GetFileName(dir);
                    var newDirName = folderNameCleanupRegex.Replace(dirName, string.Empty).Trim();

                    if (newDirName != dirName)
                    {
                        var parentDir = Path.GetDirectoryName(dir);
                        var newDirPath = Path.Combine(parentDir, newDirName);
                        Directory.Move(dir, newDirPath);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static void SetFolderNames()
        {
            string path = Directory.GetCurrentDirectory();
            var allDirectories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

            foreach (var albumFolderPath in allDirectories)
            {
                var albumDirectoryName = Path.GetFileName(albumFolderPath);
                var albumDIrectoryMatch = albumDirectoryNameRegex.Match(albumDirectoryName);
                if (albumDIrectoryMatch.Success)
                {
                    try
                    {
                        var folderTagsMatch = folderNameKeepTagsRegex.Match(albumDirectoryName.Substring(7));

                        var correctAlbumName = folderTagsMatch.Success
                            ? albumDirectoryName.Substring(7).Replace(folderTagsMatch.ToString(), string.Empty).Trim()
                            : albumDirectoryName.Substring(7);

                        string[] cdFoldersInAlbumFolder = System.IO.Directory.GetDirectories(albumFolderPath, "*.*", SearchOption.TopDirectoryOnly);

                        if (cdFoldersInAlbumFolder.Length == 0)
                        {
                            changePictureName(albumFolderPath, correctAlbumName);
                        }
                        else
                        {
                            foreach (String cdFolder in cdFoldersInAlbumFolder)
                            {
                                var cdAlbumFolderPath = Path.Combine(albumFolderPath, cdFolder);
                                changePictureName(cdAlbumFolderPath, correctAlbumName);
                            }
                            Path.GetFileName(albumFolderPath);
                        }
                    }
                    catch (Exception)
                    {
                    }

                }
            }
        }

        private static void changePictureName(String albumFolderPath, String correctAlbumName)
        {
            string[] imageFiles = System.IO.Directory.GetFiles(albumFolderPath, "*.*", SearchOption.TopDirectoryOnly);
            string[] imageExtensions = { ".jpg", ".jpeg", ".png" };
            string[] validImageFiles = imageFiles.Where(f => imageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase) &&
                                                              (System.IO.File.GetAttributes(f) & FileAttributes.Hidden) == 0 &&
                                                              (System.IO.File.GetAttributes(f) & FileAttributes.System) == 0).ToArray();

            if (validImageFiles.Length == 1)
            {
                string imageFilePath = validImageFiles[0];
                string newImageFilePath = Path.Combine(albumFolderPath, $"{correctAlbumName}{Path.GetExtension(imageFilePath)}");
                if (imageFilePath != newImageFilePath)
                {
                    try
                    {
                        System.IO.File.Move(imageFilePath, newImageFilePath);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public static void SetMp3Tags()
        {
            string path = Directory.GetCurrentDirectory();
            var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (string filePath in allFiles)
            {
                var fileExtension = Path.GetExtension(filePath);
                if (fileExtension == ".mp3")
                {
                    try
                    {
                        // Make the file writable
                        System.IO.File.SetAttributes(filePath, System.IO.File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);

                        string[] filePathSplitted = filePath.Split(Path.DirectorySeparatorChar);
                        var len = filePathSplitted.Length;

                        var albumFolderName = filePathSplitted[len - 2];
                        var albumDirectoryHasFoldersWithCds = albumDirectoryNameRegex.IsMatch(albumFolderName);
                        if (!albumDirectoryHasFoldersWithCds)
                        {
                            len = len - 1; // Look one level up if the album folder might contain multiple CDs
                            albumFolderName = filePathSplitted[len - 2];
                        }
                            

                        var fileName = Path.GetFileNameWithoutExtension(filePath);

                        var fileData = new FileData
                        {
                            artists = new string[1],
                            genres = new string[1]
                        };

                        bool isAlbumSplit = albumFolderName.Contains("(Split)");

                        if (!isAlbumSplit)
                        {
                            fileData.trackName = fileName.Substring(5);
                            fileData.artists[0] = filePathSplitted[len - 3];
                        }
                        else // e.g., "08 - Malum - Desecrating the False Temples"
                        {
                            var index = fileName.IndexOf('-', fileName.IndexOf('-') + 1);
                            fileData.trackName = fileName.Substring(index + 2); // 2 characters after the second hyphen
                            fileData.artists[0] = fileName.Substring(5, index - 6); // Between the first and second hyphens
                        }

                        fileData.trackNumber = Convert.ToUInt32(fileName.Substring(0, 2));

                        var albumDirectoryName = filePathSplitted[len - 2];
                        fileData.albumYear = Convert.ToUInt32(albumDirectoryName.Substring(0, 4));

                        var folderTagsMatch = folderNameKeepTagsRegex.Match(albumDirectoryName.Substring(7));

                        var correctAlbumName = folderTagsMatch.Success
                            ? albumDirectoryName.Substring(7).Replace(folderTagsMatch.ToString(), string.Empty).Trim()
                            : albumDirectoryName.Substring(7);

                        string albumDirectoryNamePath = string.Join(Path.DirectorySeparatorChar.ToString(), filePathSplitted.Take(len - 1));
                        string[] cdFoldersInAlbumFolder = System.IO.Directory.GetDirectories(albumDirectoryNamePath, "*.*", SearchOption.TopDirectoryOnly);

                        if (cdFoldersInAlbumFolder.Length == 0)
                        {
                            fileData.albumName = correctAlbumName;
                        }
                        else
                        {
                            {
                                var cdAlbumFolderName = filePathSplitted[len - 1];
                                var correctCdAlbumName = correctAlbumName + " - " + cdAlbumFolderName;
                                fileData.albumName = correctCdAlbumName;
                            }
                        }

                        fileData.genres[0] = filePathSplitted[len - 4];

                        // Use TagLib.File with full qualification
                        TagLib.File tagFile = TagLib.File.Create(filePath);

                        tagFile.Tag.Track = fileData.trackNumber;
                        tagFile.Tag.Title = fileData.trackName;
                        tagFile.Tag.Year = fileData.albumYear;
                        tagFile.Tag.Album = fileData.albumName;
                        tagFile.Tag.AlbumArtists = fileData.artists;
                        tagFile.Tag.Performers = fileData.artists;
                        tagFile.Tag.Genres = fileData.genres;

                        tagFile.Save();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            MessageBox.Show("Zakończono przetwarzanie", "Informacja");
        }
    }

    public class FileData
    {
        public uint trackNumber { get; set; }
        public string trackName { get; set; }
        public uint albumYear { get; set; }
        public string albumName { get; set; }
        public string[] artists { get; set; }
        public string[] genres { get; set; }
    }
}
