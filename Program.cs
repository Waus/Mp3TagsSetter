using System;
using System.IO;
using System.Text.RegularExpressions;

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

                        var fileName = "";
                        var trackNumber = "";
                        var trackName = "";
                        var albumDirectoryName = "";
                        var albumYear = "";
                        var albumName = "";
                        var artist = "";
                        var genre = "";

                        fileName = Path.GetFileNameWithoutExtension(filePath);

                        bool isAlbumSplit = albumDirectoryNameTest.Contains("(Split)");

                        if (!isAlbumSplit)
                        {
                            trackName = fileName.Substring(5);
                            artist = filePathSplitted[len - 3];
                        }
                        else //np. "08 - Malum - Desecrating the False Temples"
                        {
                            var index = fileName.IndexOf('-', fileName.IndexOf('-') + 1);
                            trackName = fileName.Substring(index + 2); // 2 znaki po drugim myślniku
                            artist = fileName.Substring(5, index - 6); // między pierwszym a drugim myślnikiem
                        }

                        trackNumber = fileName.Substring(0, 2);

                        albumDirectoryName = filePathSplitted[len - 2];
                        albumYear = albumDirectoryName.Substring(0, 4);
                        albumName = albumDirectoryName.Substring(7);

                        genre = filePathSplitted[len - 4];

                        string[] artists = new string[1];
                        artists[0] = artist;
                        string[] genres = new string[1];
                        genres[0] = genre;

                        var file = TagLib.File.Create(filePath);

                        file.Tag.Track = Convert.ToUInt32(trackNumber);
                        file.Tag.Title = trackName;
                        file.Tag.Year = Convert.ToUInt32(albumYear);
                        file.Tag.Album = albumName;
                        file.Tag.AlbumArtists = artists;
                        file.Tag.Performers = artists;
                        file.Tag.Genres = genres;

                        file.Save();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
