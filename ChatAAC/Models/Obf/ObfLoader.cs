using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf
{
    // Klasa do parsowania plików OBF
    public static partial class ObfLoader
    {
        private static readonly HttpClient HttpClient = new();

        public static string ObfCacheDirectory { get; set; } 
        public static string PictogramsCacheDirectory { get; set; }

        static ObfLoader()
        {
            ObfCacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChatAAC",
                "Cache",
                "Obf");

            // Ścieżka do katalogu Cache/Pictograms
            PictogramsCacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChatAAC",
                "Cache",
                "Pictograms");
        }
        /// <summary>
        /// Ładuje plik OBF, aktualizuje ścieżki obrazów i kopiuje plik do katalogu Cache/Obf, jeśli jeszcze tam nie istnieje.
        /// </summary>
        /// <param name="filePath">Ścieżka do pliku OBF.</param>
        /// <returns>Zaktualizowany obiekt ObfFile lub null w przypadku błędu.</returns>
        public static async Task<ObfFile?> LoadObfAsync(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new IntFromStringConverter()
                }
            };
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("Ścieżka do pliku OBF jest pusta.");
                return null;
            }

            try
            {
                // Ścieżka do katalogu Cache/Obf
             

                // Upewnij się, że katalog Cache/Obf istnieje
                Directory.CreateDirectory(ObfCacheDirectory);

                // Nazwa pliku
                var fileName = Path.GetFileName(filePath);
                var destinationPath = Path.Combine(ObfCacheDirectory, fileName);

                // Kopiowanie pliku do Cache/Obf, jeśli nie istnieje
                if (!File.Exists(destinationPath))
                {
                    File.Copy(filePath, destinationPath);
                    Console.WriteLine($"Skopiowano plik OBF do {destinationPath}");
                }
                else
                {
                    Console.WriteLine($"Plik OBF już istnieje w {destinationPath}. Kopiowanie pominięte.");
                }

                // Wczytaj zawartość pliku OBF
                var jsonString = await File.ReadAllTextAsync(destinationPath);
                var obfFile = JsonSerializer.Deserialize<ObfFile>(jsonString, options);

                if (obfFile != null) return await UpdateImagesInButtonsAsync(obfFile, PictogramsCacheDirectory);
                Console.WriteLine("Deserializacja pliku OBF zakończyła się niepowodzeniem.");
                return null;

                // Aktualizuj ścieżki obrazów w przyciskach
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas ładowania pliku OBF: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Aktualizuje ścieżki obrazów w przyciskach, zapisuje obrazy do Cache/Pictograms i aktualizuje ścieżki lokalne.
        /// </summary>
        /// <param name="obfFile">Obiekt ObfFile do zaktualizowania.</param>
        /// <param name="cacheDirectory">Katalog Cache/Pictograms.</param>
        /// <returns>Zaktualizowany obiekt ObfFile lub null w przypadku błędu.</returns>
        private static async Task<ObfFile?> UpdateImagesInButtonsAsync(ObfFile? obfFile, string cacheDirectory)
        {
            if (obfFile == null)
            {
                Console.WriteLine("Obiekt ObfFile jest null.");
                return null;
            }

            try
            {
                // Upewnij się, że katalog Cache/Pictograms istnieje
                Directory.CreateDirectory(cacheDirectory);

                foreach (var button in obfFile.Buttons)
                {
                    // Znajdź odpowiadający obraz
                    button.Image = obfFile.Images.Find(image => image.Id == button.ImageId);
                    if (button.Image != null)
                    {
                        // Zapisz obraz do pliku i zaktualizuj ścieżkę
                        button.Image.ImagePath = await SaveImageToFileAsync(button.Image, cacheDirectory);
                    }
                    else
                    {
                        Console.WriteLine($"Brak obrazu dla przycisku o ID: {button.Id}");
                    }
                }

                return obfFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas aktualizacji obrazów w przyciskach: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Zapisuje obraz do pliku w katalogu Cache/Pictograms, jeśli jeszcze tam nie istnieje.
        /// </summary>
        /// <param name="image">Obiekt Image do zapisania.</param>
        /// <param name="cacheDirectory">Katalog Cache/Pictograms.</param>
        /// <returns>Ścieżka do zapisanego obrazu.</returns>
        private static async Task<string> SaveImageToFileAsync(Image image, string cacheDirectory)
        {
            try
            {
                // Określ rozszerzenie pliku
                var extension = GetFileExtension(image);
                var fileName = $"{image.Id}{extension}";
                var filePath = Path.Combine(cacheDirectory, fileName);

                // Sprawdź, czy plik już istnieje
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Plik obrazu już istnieje: {filePath}. Pomijanie zapisu.");
                    return filePath;
                }

                // Zapisz obraz na podstawie danych lub URL
                if (!string.IsNullOrEmpty(image.Data) && image.Data.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    await SaveImageFromBase64Async(image, filePath);
                }
                else if (!string.IsNullOrEmpty(image.Url))
                {
                    await SaveImageFromUrlAsync(image, filePath);
                }
                else
                {
                    Console.WriteLine($"Brak danych do zapisania dla obrazu o ID: {image.Id}");
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisywania obrazu: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Zapisuje obraz z danych Base64 do pliku.
        /// </summary>
        /// <param name="image">Obiekt Image zawierający dane Base64.</param>
        /// <param name="filePath">Ścieżka do pliku docelowego.</param>
        /// <returns>Task reprezentujący operację.</returns>
        private static async Task SaveImageFromBase64Async(Image image, string filePath)
        {
            try
            {
                var base64Data = ExtractBase64Data(image.Data);
                var imageBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(filePath, imageBytes);
                Console.WriteLine($"Zapisano obraz Base64 do {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisywania obrazu z Base64: {ex.Message}");
            }
        }

        /// <summary>
        /// Zapisuje obraz z URL do pliku.
        /// </summary>
        /// <param name="image">Obiekt Image zawierający URL.</param>
        /// <param name="filePath">Ścieżka do pliku docelowego.</param>
        /// <returns>Task reprezentujący operację.</returns>
        private static async Task SaveImageFromUrlAsync(Image image, string filePath)
        {
            try
            {
                var response = await HttpClient.GetAsync(image.Url);
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    Console.WriteLine($"Pobrano i zapisano obraz z URL: {filePath}");
                }
                else
                {
                    Console.WriteLine($"Nie udało się pobrać obrazu z URL: {image.Url}. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas pobierania obrazu z URL: {ex.Message}");
            }
        }

        /// <summary>
        /// Ekstrahuje dane Base64 z danych obrazu.
        /// </summary>
        /// <param name="data">Dane obrazu w formacie Base64.</param>
        /// <returns>String zawierający czyste dane Base64.</returns>
        private static string ExtractBase64Data(string data)
        {
            var match = MyRegex().Match(data);
            if (match.Success)
            {
                return match.Groups["base64"].Value;
            }

            throw new ArgumentException("Nie udało się wyodrębnić danych Base64 z obrazu.");
        }

        /// <summary>
        /// Określa rozszerzenie pliku na podstawie typu obrazu.
        /// </summary>
        /// <param name="image">Obiekt Image.</param>
        /// <returns>Rozszerzenie pliku jako string.</returns>
        private static string GetFileExtension(Image image)
        {
            if (!string.IsNullOrEmpty(image.Data))
            {
                var match = MyRegex().Match(image.Data);
                if (match.Success)
                {
                    var type = match.Groups["type"].Value.ToLower();
                    return type switch
                    {
                        "png" => ".png",
                        "jpeg" => ".jpg",
                        "jpg" => ".jpg",
                        "svg+xml" => ".svg",
                        _ => throw new ArgumentException($"Nieobsługiwany typ obrazu: {type}")
                    };
                }
            }

            if (!string.IsNullOrEmpty(image.Url))
            {
                var extension = Path.GetExtension(new Uri(image.Url).AbsolutePath);
                if (!string.IsNullOrEmpty(extension))
                {
                    return extension;
                }
            }

            throw new ArgumentException("Nie można określić rozszerzenia pliku obrazu.");
        }

        [GeneratedRegex(@"data:image/(?<type>\w+);base64,(?<base64>.+)")]
        private static partial Regex MyRegex();
    }
}