using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models;

namespace ChatAAC.Services;

public class PictogramService
{
    private static readonly HttpClient HttpClient = new();
    private readonly string _cacheDirectory;

    public PictogramService()
    {
        // Path to the cache directory
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatAAC",
            "Cache",
            "Pictograms");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<List<Pictogram>?> GetAllPictogramsAsync()
    {
        var cacheFile = Path.Combine(_cacheDirectory, "pictograms.json");

        // Check if the data is already in the cache
        if (File.Exists(cacheFile))
            try
            {
                var cachedData = await File.ReadAllTextAsync(cacheFile).ConfigureAwait(false);
                var pictograms = JsonSerializer.Deserialize<List<Pictogram>>(cachedData);
                // Verify that deserialization returned data
                if (pictograms is { Count: > 0 })
                {
                    await CheckAndDownloadMissingImages(pictograms);
                    return pictograms;
                }
                // If the data is empty, delete the cache file and download again
                File.Delete(cacheFile);
            }
            catch (JsonException ex)
            {
                Console.WriteLine(Resources.PictogramService_GetAllPictogramsAsync_Błąd_podczas_deserializacji_pliku_cache___0_, ex.Message);
                File.Delete(cacheFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(Resources.PictogramService_GetAllPictogramsAsync_Nieoczekiwany_błąd_podczas_odczytu_pliku_cache___0_, ex.Message);
            }

        // Take data from ARASAAC API 
        try
        {
            const string url = "https://api.arasaac.org/v1/pictograms/all/pl";
            var response = await HttpClient.GetAsync(url).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new Exception(
                    string.Format(Resources.PictogramServiceErrorArasaac, response.StatusCode));
            var responseData = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            await File.WriteAllTextAsync(cacheFile, responseData).ConfigureAwait(false);

            var pictograms = JsonSerializer.Deserialize<List<Pictogram>>(responseData);

            if (pictograms is not { Count: > 0 }) throw new Exception(Resources.PictogramServiceEmptyArasaac);
                    
            await CheckAndDownloadMissingImages(pictograms);

            return pictograms;
           
        }
        catch (Exception ex)
        {
            Console.WriteLine(Resources.PictogramService_GetAllPictogramsAsync_Błąd_podczas_pobierania_piktogramów_z_API___0_, ex.Message);

            return [];
        }
    }

    private async Task CheckAndDownloadMissingImages(List<Pictogram> pictograms)
    {
        foreach (var pictogram in from pictogram in pictograms
                 let imagePath = Path.Combine(_cacheDirectory, $"{pictogram.Id}.png")
                 where !File.Exists(imagePath)
                 select pictogram)
            await DownloadPictogramImageAsync(pictogram.Id.ToString());
    }

    private async Task DownloadPictogramImageAsync(string pictogramId)
    {
        var imageUrl = $"https://static.arasaac.org/pictograms/{pictogramId}/{pictogramId}_500.png";
        var imagePath = Path.Combine(_cacheDirectory, $"{pictogramId}.png");

        if (!File.Exists(imagePath))
            try
            {
                var response = await HttpClient.GetAsync(imageUrl).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var imageData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    await File.WriteAllBytesAsync(imagePath, imageData).ConfigureAwait(false);
                }
                else
                {
                    AppLogger.LogInfo(string.Format(
                        Resources.PictogramService_DownloadPictogramImageAsync_Nie_udało_się_pobrać_obrazu_piktogramu_o_ID__0___Status_Code___1_, pictogramId, response.StatusCode));
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogInfo(string.Format(Resources.PictogramService_DownloadPictogramImageAsync_Błąd_podczas_pobierania_obrazu_piktogramu__0____1_, pictogramId, ex.Message));
            }
        else
            AppLogger.LogInfo(string.Format(Resources.PictogramService_DownloadPictogramImageAsync_Obraz_piktogramu__0__już_istnieje_w_cache_, pictogramId));
    }
}