using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatAAC.Models.Obf;

public static partial class ObfLoader
{
    private static readonly HttpClient HttpClient = new();

    private static string ObfCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Obf");

    private static string PictogramsCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Pictograms");

    /// <summary>
    /// Asynchronously loads an OBF file, processes images, and returns the deserialized object.
    /// If previous data exists in the cache, it will be overwritten.
    /// </summary>
    /// <param name="filePath">The path to the OBF file to be loaded.</param>
    /// <returns>
    /// An asynchronous task that returns an ObfFile object if successful.
    /// Returns null if loading or processing fails.
    /// </returns>
    public static async Task<ObfFile?> LoadObfAsync(string? filePath)
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
            
            // Deserialize OBF file
            var obfFile = await DeserializeObfFileAsync(filePath);
            if (obfFile == null)
            {
                LogError("Failed to deserialize the OBF file.");
                return null;
            }

            // Get base directory of the original OBF file
            var obfBaseDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;

            // Process images in the OBF file, passing the base directory
            await ProcessImagesAsync(obfFile, obfBaseDirectory);

            foreach (var button in obfFile.Buttons)
            {
                // Match the corresponding image
                button.Image = obfFile.Images.Find(image => image.Id == button.ImageId);
                if (button.Image == null)
                    Console.WriteLine($"No image found for button with ID: {button.Id}");
                if (string.IsNullOrEmpty(button.Label))
                    Console.WriteLine($"Missing label for button with ID: {button.Id}");
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
    /// Deserializes the OBF file to an ObfFile object.
    /// </summary>
    /// <param name="obfFilePath">The path to the OBF file to be deserialized.</param>
    /// <returns>
    /// An asynchronous task that returns an ObfFile object if deserialization is successful.
    /// Returns null if deserialization fails.
    /// </returns>
    private static async Task<ObfFile?> DeserializeObfFileAsync(string? obfFilePath)
    {
        if (string.IsNullOrWhiteSpace(obfFilePath))
        {
            LogError("The file path is null or empty.");
            return null;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(obfFilePath);
            return JsonSerializer.Deserialize<ObfFile>(jsonString);
        }
        catch (Exception ex)
        {
            LogError($"Error deserializing OBF file: {ex.Message}");
            return null;
        }
    }


    /// <summary>
    /// Processes images in the OBF file, saving them to the cache directory.
    /// </summary>
    /// <param name="obfFile">The OBF file containing the images to be processed.</param>
    /// <param name="obfBaseDirectory">The base directory of the original OBF file.</param>
    private static async Task ProcessImagesAsync(ObfFile obfFile, string obfBaseDirectory)
    {
        foreach (var image in obfFile.Images)
        {
            var imageFileName = GenerateImageFileName(image);
            image.ImagePath = Path.Combine(PictogramsCacheDirectory, imageFileName);

            if (File.Exists(image.ImagePath))
            {
                Log($"Image already cached: {image.ImagePath}");
                continue;
            }

            if (!string.IsNullOrEmpty(image.Data) || !string.IsNullOrWhiteSpace(image.DataUrl))
                if (await SaveImageFromDataAsync(image, image.ImagePath))
                    continue;

            if (!string.IsNullOrEmpty(image.Url))
                if (await SaveImageFromUrlAsync(image, image.ImagePath))
                    continue;

            if (await SaveImageFromPathAsync(image, image.ImagePath, obfBaseDirectory)) continue;
            LogError($"Failed to save image: {image.Id}");
        }
    }

 

    /// <summary>
    /// Generates a unique filename for the image based on its ID and file extension.
    /// </summary>
    /// <param name="image">The Image object containing the image information.</param>
    /// <returns>
    /// A string representing the generated filename for the image.
    /// The filename consists of a sanitized version of the image ID followed by the appropriate file extension.
    /// </returns>
    private static string GenerateImageFileName(Image image)
    {
        var extension = GetFileExtension(image);
        var imageIdSanitized = Regex.Replace(image.Id, @"[^\w\-]", "_");
        return $"{imageIdSanitized}{extension}";
    }

    /// <summary>
    /// Attempts to save an image from Base64 data or by downloading it from a URL.
    /// </summary>
    /// <param name="image">The Image object containing the image data or URL.</param>
    /// <param name="destinationPath">The file path where the image should be saved.</param>
    /// <returns>
    /// A boolean value indicating whether the image was successfully saved.
    /// Returns true if the image was saved, false otherwise.
    /// </returns>
    /// <remarks>
    /// This method first checks if the image has a DataUrl. If so, it attempts to download the data from that URL.
    /// If not, it uses the existing Data property. The method then converts the Base64 data to bytes and saves it to the specified path.
    /// </remarks>
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
    /// Attempts to save the image by downloading it from a URL.
    /// </summary>
    /// <param name="image">The Image object containing the URL of the image to be downloaded.</param>
    /// <param name="destinationPath">The file path where the downloaded image should be saved.</param>
    /// <returns>
    /// An asynchronous task that returns a boolean value indicating whether the image was successfully saved.
    /// Returns true if the image was saved, false otherwise.
    /// </returns>
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
    /// Attempts to save the image by downloading it from a path, using only the file name.
    /// </summary>
    /// <param name="image">The Image object containing the path of the image to be saved.</param>
    /// <param name="destinationPath">The file path where the downloaded image should be saved.</param>
    /// <param name="obfBaseDirectory">The base directory of the original OBF file.</param>
    /// <returns>
    /// An asynchronous task that returns a boolean value indicating whether the image was successfully saved.
    /// Returns true if the image was saved, false otherwise.
    /// </returns>
    private static async Task<bool> SaveImageFromPathAsync(Image image, string destinationPath,
        string obfBaseDirectory)
    {
        if (string.IsNullOrEmpty(image.Path))
            return false;

        try
        {
            // Sanitize the image path to prevent directory traversal attacks
            var sanitizedImagePath = SanitizeImagePath(image.Path);

            // Build the full path to the image relative to the OBF base directory
            var fullImagePath = Path.Combine(obfBaseDirectory, sanitizedImagePath);
            fullImagePath = Path.GetFullPath(fullImagePath);

            // Ensure that the image path is within the OBF base directory
            var fullObfBaseDirectory = Path.GetFullPath(obfBaseDirectory);
            if (!fullImagePath.StartsWith(fullObfBaseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                LogError($"Detected a potentially unsafe image path: {image.Path}");
                return false;
            }

            // Check asynchronously if the file exists
            var fileExists = await Task.Run(() => File.Exists(fullImagePath));
            if (fileExists)
            {
                // Copy the file asynchronously
                await CopyFileAsync(fullImagePath, destinationPath);
                Log($"Copied image from {fullImagePath} to {destinationPath}");
                return true;
            }

            // Try using only the file name in known directories
            var imageFileName = Path.GetFileName(sanitizedImagePath);

            var potentialPaths = new[]
            {
                Path.Combine(PictogramsCacheDirectory, imageFileName),
                Path.Combine(ObfCacheDirectory, imageFileName)
            };

            foreach (var path in potentialPaths)
            {
                var exists = await Task.Run(() => File.Exists(path));
                if (!exists) continue;
                await CopyFileAsync(path, destinationPath);
                Log($"Copied image from {path} to {destinationPath}");
                return true;
            }

            LogError($"Image file not found: {fullImagePath}");
            return false;
        }
        catch (Exception ex)
        {
            LogError($"Error saving image from path: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Asynchronously copies a file from the source path to the destination path.
    /// If the destination directory does not exist, it is created.
    /// </summary>
    /// <param name="sourceFilePath">The path of the source file to be copied.</param>
    /// <param name="destinationFilePath">The path where the file should be copied.</param>
    /// <returns>
    /// An asynchronous task that completes when the file is copied.
    /// </returns>
    private static async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
    {
        const int bufferSize = 81920; // Default buffer size

        // Ensure the destination directory exists
        var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize, true);
        await using var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize, true);
        await sourceStream.CopyToAsync(destinationStream);
    }

    /// <summary>
    /// Sanitizes an image path by replacing backslashes with slashes and removing any potentially unsafe components.
    /// </summary>
    /// <param name="imagePath">The original image path.</param>
    /// <returns>The sanitized image path.</returns>
    private static string SanitizeImagePath(string imagePath)
    {
        // Replace backslashes with slashes
        imagePath = imagePath.Replace('\\', '/');

        // Split the image path into segments
        var segments = imagePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        // Filter out any potentially unsafe components (e.g., ".", "..")
        var sanitizedSegments = segments.Where(segment => segment != "." && segment != "..");

        // Combine the sanitized segments back into a single path
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

        if (!string.IsNullOrEmpty(image.Url)) return Path.GetExtension(new Uri(image.Url).AbsolutePath);

        if (!string.IsNullOrEmpty(image.Path)) return Path.GetExtension(image.Path);

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
    /// Ensures a directory exists.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
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
    /// Logs a message to the console.
    /// </summary>
    private static void Log(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Logs an error message to the console.
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