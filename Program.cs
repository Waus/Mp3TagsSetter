using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Mp3TagsSetter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            SetMp3Tags();
        }

        public static void SetMp3Tags()
        {
            Regex albumDirectoryNameRegex = new Regex(@"^\d\d\d\d\s\-\s[\w* [(\.]+"); // "2020 - Tytuł"

            string path = Directory.GetCurrentDirectory();
            var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (string filePath in allFiles)
            {
                var fileExtension = Path.GetExtension(filePath);
                if (fileExtension == ".mp3")
                {
                    try
                    {
                        string[] filePathSplitted = filePath.Split('\\');
                        var len = filePathSplitted.Length;

                        var albumDirectoryNameTest = filePathSplitted[len - 2];
                        if (!albumDirectoryNameRegex.IsMatch(albumDirectoryNameTest))
                            len = len - 1; //patrzymy poziom wyżej, bo w albumie mogły być np. 2 CD w osobnych folderach

                        var fileName = Path.GetFileNameWithoutExtension(filePath);

                        var fileData = new FileData();

                        bool isAlbumSplit = albumDirectoryNameTest.Contains("(Split)");

                        if (!isAlbumSplit)
                        {
                            fileData.trackName = fileName.Substring(5);
                            fileData.artists[0] = filePathSplitted[len - 3];
                        }
                        else //np. "08 - Malum - Desecrating the False Temples"
                        {
                            var index = fileName.IndexOf('-', fileName.IndexOf('-') + 1);
                            fileData.trackName = fileName.Substring(index + 2); // 2 znaki po drugim myślniku
                            fileData.artists[0] = fileName.Substring(5, index - 6); // między pierwszym a drugim myślnikiem
                        }

                        fileData.trackNumber = Convert.ToUInt32(fileName.Substring(0, 2));

                        var albumDirectoryName = filePathSplitted[len - 2];
                        fileData.albumYear = Convert.ToUInt32(albumDirectoryName.Substring(0, 4));
                        fileData.albumName = albumDirectoryName.Substring(7);

                        fileData.genres[0] = filePathSplitted[len - 4];

                        var file = TagLib.File.Create(filePath);

                        file.Tag.Track = fileData.trackNumber;
                        file.Tag.Title = fileData.trackName;
                        file.Tag.Year = fileData.albumYear;
                        file.Tag.Album = fileData.albumName;
                        file.Tag.AlbumArtists = fileData.artists;
                        file.Tag.Performers = fileData.artists;
                        file.Tag.Genres = fileData.genres;

                        file.Save();
                    }
                    catch
                    {
                    }
                }
            }
            MessageBox.Show("Zakończono przetwarzanie", "Informacja");
        }
    }
}
