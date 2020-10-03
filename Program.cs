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

                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var trackNumber = fileName.Substring(0, 2);
                        var trackName = fileName.Substring(4);

                        var albumDirectoryNameTest = filePathSplitted[len - 2];
                        if (!albumDirectoryNameRegex.IsMatch(albumDirectoryNameTest))
                            len = len - 1; //patrzymy poziom wyżej, bo w albumie mogły być np. 2 CD w osobnych folderach

                        var albumDirectoryName = filePathSplitted[len - 2];
                        var albumYear = albumDirectoryName.Substring(0, 4);
                        var albumName = albumDirectoryName.Substring(6);

                        var artist = filePathSplitted[len - 3];
                        var genre = filePathSplitted[len - 4];

                        string[] artists = new string[1];
                        artists[0] = artist;
                        string[] genres = new string[1];
                        genres[0] = genre;

                        //taglib-sharp
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
