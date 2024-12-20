using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf;

public static partial class ObfLoader
{
    private static readonly HttpClient HttpClient = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new StringConverterAllowNumber(),
            new IntFromStringConverter()
        }
    };

    public static string ObfCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Obf");

    public static string PictogramsCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Pictograms");

    /// <summary>
    ///     Loads an OBF file, processes images, and returns the deserialized object.
    /// </summary>
    public static async Task<ObfFile?> LoadObfAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            LogError("The OBF file path is empty.");
            return null;
        }

        try
        {
            // Ensure cache directories exist
            EnsureDirectoryExists(ObfCacheDirectory);
            EnsureDirectoryExists(PictogramsCacheDirectory);

            // Copy OBF file to cache directory
            var obfFileName = Path.GetFileName(filePath);
            var cachedObfPath = Path.Combine(ObfCacheDirectory, obfFileName);
            await CopyFileIfNeededAsync(filePath, cachedObfPath);

            // Deserialize OBF file
            var obfFile = await DeserializeObfFileAsync(cachedObfPath);
            if (obfFile == null)
            {
                LogError("Failed to deserialize the OBF file.");
                return null;
            }


            // Pobierz katalog bazowy oryginalnego pliku OBF
            var obfBaseDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;

            // Przetwarzaj obrazy w pliku OBF, przekazując katalog bazowy
            await ProcessImagesAsync(obfFile, obfBaseDirectory);


            foreach (var button in obfFile.Buttons)
            {
                // Znajdź odpowiadający obraz
                button.Image = obfFile.Images.Find(image => image.Id == button.ImageId);
                if (button.Image == null) Console.WriteLine($"Brak obrazu dla przycisku o ID: {button.Id}");
            }

            return obfFile;
        }
        catch (Exception ex)
        {
            LogError($"Error loading OBF file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Deserializes the OBF file to an ObfFile object.
    /// </summary>
    private static async Task<ObfFile?> DeserializeObfFileAsync(string obfFilePath)
    {
        try
        {
            var jsonString = await File.ReadAllTextAsync(obfFilePath);
            return JsonSerializer.Deserialize<ObfFile>(jsonString, JsonOptions);
        }
        catch (Exception ex)
        {
            LogError($"Error deserializing OBF file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Processes images in the OBF file, saving them to the cache directory.
    /// </summary>
    private static async Task ProcessImagesAsync(ObfFile obfFile, string obfBaseDirectory)
    {
        foreach (var image in obfFile.Images)
        {
            // Generate a unique filename for the image
            var imageFileName = GenerateImageFileName(image);

            // Set the ImagePath to the cached image path
            image.ImagePath = Path.Combine(PictogramsCacheDirectory, imageFileName);

            // Skip if the image is already cached
            if (File.Exists(image.ImagePath))
            {
                Log($"Image already cached: {image.ImagePath}");
                continue;
            }

            // Próba zapisania obrazu z różnych źródeł
            if (!string.IsNullOrEmpty(image.Data) || !string.IsNullOrWhiteSpace(image.DataUrl))
                if (await SaveImageFromDataAsync(image, image.ImagePath))
                    continue;

            if (!string.IsNullOrEmpty(image.Url))
                if (await SaveImageFromUrlAsync(image, image.ImagePath))
                    continue;

            if (await SaveImageFromPathAsync(image, image.ImagePath, obfBaseDirectory)) continue;
            LogError($"Nie udało się zapisać obrazu: {image.Id}");
        }
    }

    /// <summary>
    ///     Generates a unique filename for the image based on its ID.
    /// </summary>
    private static string GenerateImageFileName(Image image)
    {
        var extension = GetFileExtension(image);
        var imageIdSanitized = Regex.Replace(image.Id, @"[^\w\-]", "_");
        return $"{imageIdSanitized}{extension}";
    }

    /// <summary>
    ///     Attempts to save the image from Base64 data.
    /// </summary>
    private static async Task<bool> SaveImageFromDataAsync(Image image, string destinationPath)
    {
        var data = image.Data;

        if (!string.IsNullOrEmpty(image.DataUrl))
        {
            var response = await HttpClient.GetAsync(image.DataUrl);
            if (response.IsSuccessStatusCode)
            {
                data = await response.Content.ReadAsStringAsync();
                Log($"Downloaded image data from URL: {destinationPath}");
            }
        }

        if (string.IsNullOrEmpty(data)) return false;

        try
        {
            var base64Data = ExtractBase64Data(data);
            var imageBytes = Convert.FromBase64String(base64Data);
            await File.WriteAllBytesAsync(destinationPath, imageBytes);
            Log($"Saved image from Base64 data: {destinationPath}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error saving image from Base64 data: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Attempts to save the image by downloading it from a URL.
    /// </summary>
    private static async Task<bool> SaveImageFromUrlAsync(Image image, string destinationPath)
    {
        if (string.IsNullOrEmpty(image.Url))
            return false;

        try
        {
            var response = await HttpClient.GetAsync(image.Url);
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(destinationPath, imageBytes);
                Log($"Downloaded and saved image from URL: {destinationPath}");
                return true;
            }

            LogError($"Failed to download image from URL: {image.Url}. Status code: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            LogError($"Error downloading image from URL: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Próbuje zapisać obraz z podanej ścieżki, używając tylko nazwy pliku.
    /// </summary>
    private static async Task<bool> SaveImageFromPathAsync(Image image, string destinationPath,
        string obfBaseDirectory)
    {
        if (string.IsNullOrEmpty(image.Path))
            return false;

        try
        {
            // Sanitizuj ścieżkę obrazu, aby zapobiec atakom typu directory traversal
            var sanitizedImagePath = SanitizeImagePath(image.Path);

            // Buduj pełną ścieżkę do obrazu względem katalogu bazowego OBF
            var fullImagePath = Path.Combine(obfBaseDirectory, sanitizedImagePath);
            fullImagePath = Path.GetFullPath(fullImagePath);

            // Upewnij się, że ścieżka obrazu znajduje się w katalogu bazowym OBF
            var fullObfBaseDirectory = Path.GetFullPath(obfBaseDirectory);
            if (!fullImagePath.StartsWith(fullObfBaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                LogError($"Wykryto niebezpieczną ścieżkę obrazu: {image.Path}");
                return false;
            }

            // Sprawdź asynchronicznie, czy plik istnieje
            var fileExists = await Task.Run(() => File.Exists(fullImagePath));
            if (fileExists)
            {
                // Skopiuj plik asynchronicznie
                await CopyFileAsync(fullImagePath, destinationPath);
                Log($"Skopiowano obraz z {fullImagePath} do {destinationPath}");
                return true;
            }

            // Spróbuj użyć tylko nazwy pliku w znanych katalogach
            var imageFileName = Path.GetFileName(sanitizedImagePath);

            var potentialPaths = new[]
            {
                Path.Combine(PictogramsCacheDirectory, imageFileName),
                Path.Combine(ObfCacheDirectory, imageFileName)
            };

            foreach (var path in potentialPaths)
            {
                var exists = await Task.Run(() => File.Exists(path));
                if (exists)
                {
                    await CopyFileAsync(path, destinationPath);
                    Log($"Skopiowano obraz z {path} do {destinationPath}");
                    return true;
                }
            }

            LogError($"Plik obrazu nie został znaleziony: {fullImagePath}");
            return false;
        }
        catch (Exception ex)
        {
            LogError($"Błąd podczas zapisywania obrazu z ścieżki: {ex.Message}");
            return false;
        }
    }

    private static async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
    {
        const int bufferSize = 81920; // Domyślny rozmiar bufora

        // Upewnij się, że katalog docelowy istnieje
        var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize, true);
        await using var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize, true);
        await sourceStream.CopyToAsync(destinationStream);
    }

    private static string SanitizeImagePath(string imagePath)
    {
        // Zamień backslashes na slashe
        imagePath = imagePath.Replace('\\', '/');

        // Usuń wszelkie niebezpieczne komponenty ścieżki, takie jak '../' lub './'
        var segments = imagePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var sanitizedSegments = new List<string>();
        foreach (var segment in segments)
        {
            if (segment == "." || segment == "..")
                // Pomijamy niebezpieczne segmenty
                continue;

            sanitizedSegments.Add(segment);
        }

        // Ponownie łączymy zsanityzowane segmenty
        return Path.Combine(sanitizedSegments.ToArray());
    }

    /// <summary>
    ///     Extracts Base64 data from a data URI.
    /// </summary>
    private static string ExtractBase64Data(string data)
    {
        var match = DataUriRegex().Match(data);
        if (match.Success) return match.Groups["base64"].Value;

        throw new ArgumentException("Failed to extract Base64 data from image.");
    }

    /// <summary>
    ///     Determines the file extension for the image.
    /// </summary>
    private static string GetFileExtension(Image image)
    {
        if (!string.IsNullOrEmpty(image.Data))
        {
            var match = DataUriRegex().Match(image.Data);
            if (match.Success) return GetExtensionFromMimeType(match.Groups["type"].Value);
        }

        if (!string.IsNullOrEmpty(image.Url)) return Path.GetExtension(new Uri(image.Url).AbsolutePath) ?? ".png";

        if (!string.IsNullOrEmpty(image.Path)) return Path.GetExtension(image.Path) ?? ".png";

        return ".png"; // Default extension
    }

    /// <summary>
    ///     Maps MIME types to file extensions.
    /// </summary>
    private static string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType.ToLower() switch
        {
            "png" => ".png",
            "jpeg" or "jpg" => ".jpg",
            "svg+xml" => ".svg",
            _ => ".png" // Default to .png if unknown
        };
    }

    /// <summary>
    ///     Ensures a directory exists.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <summary>
    ///     Copies a file to the destination if it doesn't already exist there.
    /// </summary>
    private static async Task CopyFileIfNeededAsync(string sourcePath, string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            await Task.Run(() => File.Copy(sourcePath, destinationPath));
            Log($"Copied file to {destinationPath}");
        }
        else
        {
            Log($"File already exists at {destinationPath}. Copying skipped.");
        }
    }

    /// <summary>
    ///     Logs a message to the console.
    /// </summary>
    private static void Log(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    ///     Logs an error message to the console.
    /// </summary>
    private static void LogError(string message)
    {
        Console.Error.WriteLine(message);
    }

    /// <summary>
    ///     Regular expression to extract data from data URIs.
    /// </summary>
    [GeneratedRegex(@"data:image/(?<type>\w+);base64,(?<base64>.+)")]
    private static partial Regex DataUriRegex();
}