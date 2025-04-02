using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TagLib;

namespace Mp3TagsSetter
{
    static class Program
    {
        // Regex do sprawdzenia, czy nazwa folderu to album
        static Regex albumDirectoryNameRegex = new Regex(@"^\d{4}\s\-\s[\w* [(\.]+"); // "2020 - Tytuł"
        // Regex do usunięcia "(Compilation|Single|Live|Split|2CD|3CD|4CD|5CD)" z nazwy albumu
        static Regex folderNameKeepTagsRegex = new Regex(@"\s*\((Compilation|Single|Live|Split|2CD|3CD|4CD|5CD)\)$");

        static void Main()
        {
            SetMp3Tags();

            // Komunikat końcowy
            Console.WriteLine("Zakończono przetwarzanie.\nNaciśnij dowolny klawisz, aby zamknąć okno...");
            Console.ReadKey();
        }

        public static void SetMp3Tags()
        {
            string path = Directory.GetCurrentDirectory();
            // Pobieramy wszystkie pliki
            var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

            // Filtrujemy tylko pliki .mp3
            var mp3Files = allFiles.Where(f => Path.GetExtension(f).Equals(".mp3", StringComparison.OrdinalIgnoreCase)).ToList();
            int totalCount = mp3Files.Count;
            int processedCount = 0;

            // Przetwarzamy po kolei każdą mp3
            foreach (string filePath in mp3Files)
            {
                try
                {
                    // Usunięcie atrybutu tylko do odczytu, aby móc zapisać zmiany
                    System.IO.File.SetAttributes(
                        filePath,
                        System.IO.File.GetAttributes(filePath) & ~FileAttributes.ReadOnly
                    );

                    string[] filePathSplitted = filePath.Split(Path.DirectorySeparatorChar);
                    var len = filePathSplitted.Length;

                    // Sprawdzamy, czy folder nazwy albumu wygląda na wzorzec "2020 - Nazwa"
                    var albumFolderName = filePathSplitted[len - 2];
                    var albumDirectoryHasFoldersWithCds = albumDirectoryNameRegex.IsMatch(albumFolderName);

                    if (!albumDirectoryHasFoldersWithCds)
                    {
                        // Jeżeli mamy np. folder "CD1" lub "CD2", musimy spojrzeć wyżej
                        len = len - 1;
                        albumFolderName = filePathSplitted[len - 2];
                    }

                    var fileName = Path.GetFileNameWithoutExtension(filePath);

                    var fileData = new FileData
                    {
                        artists = new string[1],
                        genres = new string[1]
                    };

                    // Czy to jest split?
                    bool isAlbumSplit = albumFolderName.Contains("(Split)");

                    if (!isAlbumSplit)
                    {
                        // Nazwa utworu po "XX - "
                        fileData.trackName = fileName.Substring(5);
                        // Nazwa artysty to katalog wyżej
                        fileData.artists[0] = filePathSplitted[len - 3];
                    }
                    else
                    {
                        // Jeżeli jest "08 - Malum - Desecrating the False Temples"
                        var index = fileName.IndexOf('-', fileName.IndexOf('-') + 1);
                        fileData.trackName = fileName.Substring(index + 2);
                        fileData.artists[0] = fileName.Substring(5, index - 6);
                    }

                    // Numer utworu
                    fileData.trackNumber = Convert.ToUInt32(fileName.Substring(0, 2));

                    // Rok i surowa nazwa albumu
                    var albumDirectoryName = filePathSplitted[len - 2];
                    fileData.albumYear = Convert.ToUInt32(albumDirectoryName.Substring(0, 4));

                    // Usuwamy np. "(2CD)" z nazwy
                    var folderTagsMatch = folderNameKeepTagsRegex.Match(albumDirectoryName.Substring(7));
                    var correctAlbumName = folderTagsMatch.Success
                        ? albumDirectoryName.Substring(7)
                            .Replace(folderTagsMatch.ToString(), string.Empty)
                            .Trim()
                        : albumDirectoryName.Substring(7);

                    // Ścieżka do folderu albumu (bądź rodzica "CD1" itp.)
                    string albumDirectoryNamePath = string.Join(
                        Path.DirectorySeparatorChar.ToString(),
                        filePathSplitted.Take(len - 1)
                    );

                    // Czy album posiada podfoldery (np. CD1, CD2)?
                    string[] cdFoldersInAlbumFolder = Directory.GetDirectories(
                        albumDirectoryNamePath,
                        "*.*",
                        SearchOption.TopDirectoryOnly
                    );

                    if (cdFoldersInAlbumFolder.Length == 0)
                    {
                        fileData.albumName = correctAlbumName;
                    }
                    else
                    {
                        // Jeśli mamy foldery CD1, CD2, to w nazwie albumu dopisujemy " - CD1" itd.
                        var cdAlbumFolderName = filePathSplitted[len - 1];
                        var correctCdAlbumName = correctAlbumName + " - " + cdAlbumFolderName;
                        fileData.albumName = correctCdAlbumName;
                    }

                    // Nazwa gatunku (np. "Folk metal") z katalogu wyżej
                    fileData.genres[0] = filePathSplitted[len - 4];

                    // Tworzymy obiekt TagLib do aktualnego pliku
                    TagLib.File tagFile = TagLib.File.Create(filePath);

                    // Ustawiamy podstawowe tagi
                    tagFile.Tag.Track = fileData.trackNumber;
                    tagFile.Tag.Title = fileData.trackName;
                    tagFile.Tag.Year = fileData.albumYear;
                    tagFile.Tag.Album = fileData.albumName;
                    tagFile.Tag.AlbumArtists = fileData.artists;
                    tagFile.Tag.Performers = fileData.artists;
                    tagFile.Tag.Genres = fileData.genres;

                    // Znajdź plik graficzny w folderze albumDirectoryNamePath
                    string coverPath = FindCoverImageInFolder(albumDirectoryNamePath);

                    if (!string.IsNullOrEmpty(coverPath) && System.IO.File.Exists(coverPath))
                    {
                        // Wczytujemy plik jako tablicę bajtów
                        byte[] coverBytes = System.IO.File.ReadAllBytes(coverPath);

                        // Możemy spróbować ustalić MIME Type na podstawie rozszerzenia
                        string mimeType = GetMimeTypeForImage(coverPath);

                        TagLib.IPicture coverPicture = new TagLib.Picture
                        {
                            Data = coverBytes,
                            Type = PictureType.FrontCover,
                            Description = "Cover",
                            MimeType = mimeType
                        };

                        // Ustawiamy obrazek jako jedyny w tagach
                        tagFile.Tag.Pictures = new IPicture[] { coverPicture };
                    }

                    tagFile.Save();
                }
                catch (Exception ex)
                {
                    // Ewentualny log błędu
                    Console.WriteLine($"Błąd przetwarzania pliku {filePath}:\n{ex.Message}");
                }

                // Zwiększamy licznik przetworzonych plików i wyświetlamy postęp
                processedCount++;
                Console.WriteLine($"Przetworzono {processedCount} z {totalCount} plików MP3.");
            }
        }

        /// <summary>
        /// Funkcja pomocnicza – szuka pierwszego pliku graficznego we wskazanym folderze (jpg/png/gif/bmp, itp.)
        /// Zwraca pełną ścieżkę do pliku lub pusty string, jeśli nie znalazła.
        /// </summary>
        private static string FindCoverImageInFolder(string folderPath)
        {
            // Najpopularniejsze rozszerzenia obrazów
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

            // Znajdź wszystkie pliki w folderze, filtrując po rozszerzeniach
            var imageFiles = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                          .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                          .Where(file => {
                              var attr = System.IO.File.GetAttributes(file);
                              return (attr & FileAttributes.Hidden) == 0
                                  && (attr & FileAttributes.System) == 0;
                          })
                          .ToList();

            // Założenie: w każdym folderze jest co najwyżej 1 plik graficzny z okładką
            return imageFiles.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Pomocnicza metoda ustalająca MIME type na podstawie rozszerzenia pliku.
        /// </summary>
        private static string GetMimeTypeForImage(string imagePath)
        {
            string ext = Path.GetExtension(imagePath).ToLower();
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "image/unknown";
            }
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
