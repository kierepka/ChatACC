using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatAAC.Models.Obf;


// Class for parsing OBF files
public class ObfLoader
{

    private static readonly HttpClient HttpClient = new();

    public static async Task<ObfFile?> LoadObfAsync(string filePath)
    {
        var jsonString = await File.ReadAllTextAsync(filePath);
        var obfFile = JsonSerializer.Deserialize<ObfFile>(jsonString);
        return await UpdateImagesInButtonsAsync(obfFile);
    }

    private static async Task<ObfFile?> UpdateImagesInButtonsAsync(ObfFile? obfFile)
    {
        if (obfFile == null) return null;

        var cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatAAC",
            "Cache",
            "Pictograms");

        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

        foreach (var button in obfFile.Buttons)
        {
            button.Image = obfFile.Images.Find(image => image.Id == button.ImageId);
            if (button.Image != null)
            {
                button.Image.ImagePath = await SaveImageToFileAsync(button.Image, cacheDirectory);
            }
        }

        return obfFile;
    }
    
    private static async Task<string> SaveImageToFileAsync(Image image, string cacheDirectory)
    {
        // Ensure the cache directory exists
        Directory.CreateDirectory(cacheDirectory); // This ensures the directory is there without throwing an error if it already exists.

        
        // Determine the file extension based on the content type
        var extension = GetFileExtension(image);
        var filePath = Path.Combine(cacheDirectory, $"{image.Id}{extension}");
        // Update the image path to the locally cached file
        image.ImagePath = filePath;
        // Check if the file already exists to avoid unnecessary downloads or overwrites
        if (File.Exists(filePath))
        {
            Console.WriteLine($"File already exists: {filePath}. Skipping download.");
            return filePath;
        }
        
        if (!string.IsNullOrEmpty(image.Data) && image.Data.StartsWith($"data:image/"))
        {
            try
            {
                var startIndex = image.Data.IndexOf("base64,", StringComparison.Ordinal) + 7;
                var base64Data = image.Data.Substring(startIndex);
                var imageBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(filePath, imageBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image from Base64: {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(image.Url))
        {
            try
            {
                var response = await HttpClient.GetAsync(image.Url);
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image from URL: {ex.Message}");
            }
        }

        return filePath;
    }
    // Helper method to determine the file extension based on the content type
    // Helper method to determine the file extension based on the image's data URI or URL
    private static string GetFileExtension(Image image)
    {
        if (!string.IsNullOrEmpty(image.Data))
        {
            var match = Regex.Match(image.Data, @"data:image/(?<type>\w+);base64,");
            if (match.Success)
            {
                var type = match.Groups["type"].Value;
                return type switch
                {
                    "png" => ".png",
                    "jpeg" => ".jpg",
                    "svg+xml" => ".svg",
                    _ => throw new ArgumentException("Unsupported image data type.")
                };
            }
        }
        if (!string.IsNullOrEmpty(image.Url))
        {
            return Path.GetExtension(new Uri(image.Url).AbsolutePath);
        }

        throw new ArgumentException("Unable to determine the file extension from image data or URL.");
    }
    
}