using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TagLib;

namespace Mp3TagsSetter
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            CleanFolderNames();
            SetFolderNames();
            SetMp3Tags();
        }

        private static void CleanFolderNames()
        {
            // Regex to remove "(Demo)" and "(EP)" from folder names
            Regex folderNameCleanupRegex = new Regex(@"\s*\((Demo|EP)\)$");

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
                catch (Exception ignored)
                {
                }
            }
        }

        private static void SetFolderNames()
        {
            Regex folderNameKeepTagsRegex = new Regex(@"\s*\((Compilation|Single|Live|Split)\)$");

            string path = Directory.GetCurrentDirectory();
            var allDirectories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

            foreach (var albumFolderPath in allDirectories)
            {
                try
                {
                    var albumDirectoryName = Path.GetFileName(albumFolderPath);
                    var folderTagsMatch = folderNameKeepTagsRegex.Match(albumDirectoryName.Substring(7));

                    var correctAlbumName = folderTagsMatch.Success
                        ? albumDirectoryName.Substring(7).Replace(folderTagsMatch.ToString(), string.Empty).Trim()
                        : albumDirectoryName.Substring(7);


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
                            System.IO.File.Move(imageFilePath, newImageFilePath);
                        }
                    }
                }
                catch (Exception ignored)
                {
                }
            }
        }

        public static void SetMp3Tags()
        {
            Regex albumDirectoryNameRegex = new Regex(@"^\d\d\d\d\s\-\s[\w* [(\.]+"); // "2020 - Tytuł"
            Regex folderNameKeepTagsRegex = new Regex(@"\s*\((Compilation|Single|Live|Split)\)$");

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

                        var albumDirectoryNameTest = filePathSplitted[len - 2];
                        if (!albumDirectoryNameRegex.IsMatch(albumDirectoryNameTest))
                            len = len - 1; // Look one level up if the album folder might contain multiple CDs

                        var fileName = Path.GetFileNameWithoutExtension(filePath);

                        var fileData = new FileData
                        {
                            artists = new string[1],
                            genres = new string[1]
                        };

                        bool isAlbumSplit = albumDirectoryNameTest.Contains("(Split)");

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

                        fileData.albumName = correctAlbumName;

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
                    catch (Exception ignored)
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
        public string folderTags { get; set; }  // Added for storing tags that should appear in the folder name
    }
}
