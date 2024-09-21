using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Linq;
using ChatAAC.Models;

namespace ChatAAC.Services
{
    public class PictogramService
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly string _cacheDirectory;

        public PictogramService()
        {
            // Ścieżka do katalogu bufora
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

            // Sprawdź, czy dane są już w buforze
            if (File.Exists(cacheFile))
            {
                try
                {
                    var cachedData = await File.ReadAllTextAsync(cacheFile).ConfigureAwait(false);
                    var pictograms = JsonSerializer.Deserialize<List<Pictogram>>(cachedData);

                    // Sprawdź, czy deserializacja zwróciła dane
                    if (pictograms != null && pictograms.Count > 0)
                    {
                        Console.WriteLine("Piktogramy załadowane z cache.");
                        return pictograms;
                    }
                    else
                    {
                        Console.WriteLine("Cache jest pusty. Pobieranie danych z API.");
                        // Jeśli dane są puste, usuń plik cache i pobierz ponownie
                        File.Delete(cacheFile);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Błąd podczas deserializacji pliku cache: {ex.Message}");
                    Console.WriteLine("Usuwanie uszkodzonego pliku cache i pobieranie danych z API.");
                    // Usuń uszkodzony plik cache
                    File.Delete(cacheFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nieoczekiwany błąd podczas odczytu pliku cache: {ex.Message}");
                    // W zależności od potrzeb, możesz zdecydować, czy chcesz kontynuować pobieranie danych
                    // lub przerwać działanie aplikacji
                }
            }

            // Pobierz dane z API ARASAAC
            try
            {
                var url = "https://api.arasaac.org/v1/pictograms/all/pl";
                var response = await HttpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    // Zapisz dane do cache
                    await File.WriteAllTextAsync(cacheFile, responseData).ConfigureAwait(false);
                    Console.WriteLine("Piktogramy pobrane z API i zapisane do cache.");

                    var pictograms = JsonSerializer.Deserialize<List<Pictogram>>(responseData);

                    // Sprawdź, czy deserializacja zwróciła dane
                    if (pictograms != null && pictograms.Count > 0)
                    {
                        return pictograms;
                    }
                    else
                    {
                        throw new Exception("Pobrano puste dane z API ARASAAC.");
                    }
                }
                else
                {
                    throw new Exception(
                        $"Niepowodzenie pobierania danych z ARASAAC API. Status Code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas pobierania piktogramów z API: {ex.Message}");
                // Możesz zdecydować, jak obsłużyć ten błąd, np. zwrócić pustą listę lub ponownie wyrzucić wyjątek
                return new List<Pictogram>();
            }
        }

        public async Task DownloadPictogramImageAsync(string pictogramId)
        {
            var imageUrl = $"https://static.arasaac.org/pictograms/{pictogramId}/{pictogramId}_500.png";
            var imagePath = Path.Combine(_cacheDirectory, $"{pictogramId}.png");

            if (!File.Exists(imagePath))
            {
                try
                {
                    var response = await HttpClient.GetAsync(imageUrl).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var imageData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        await File.WriteAllBytesAsync(imagePath, imageData).ConfigureAwait(false);
                        Console.WriteLine($"Piktogram {pictogramId} pobrany i zapisany.");
                    }
                    else
                    {
                        Console.WriteLine($"Nie udało się pobrać obrazu piktogramu o ID {pictogramId}. Status Code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas pobierania obrazu piktogramu {pictogramId}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Obraz piktogramu {pictogramId} już istnieje w cache.");
            }
        }

        /// <summary>
        /// Wyodrębnia unikalne kategorie z listy piktogramów
        /// </summary>
        /// <param name="pictograms">Lista piktogramów</param>
        /// <returns>Lista unikalnych kategorii</returns>
        public List<Category> ExtractCategories(List<Pictogram> pictograms)
        {
            return pictograms
                .Where(p => !string.IsNullOrWhiteSpace(p.Category))
                .GroupBy(p => p.Category)
                .Select(g => new Category
                {
                    Id = g.Key, // Zakładam, że nazwa kategorii jest unikalnym ID
                    Name = g.Key
                })
                .OrderBy(c => c.Name)
                .ToList();
        }

        /// <summary>
        /// Wyodrębnia unikalne tagi z listy piktogramów
        /// </summary>
        /// <param name="pictograms">Lista piktogramów</param>
        /// <returns>Lista unikalnych tagów</returns>
        public List<Tag> ExtractTags(List<Pictogram> pictograms)
        {
            return pictograms
                .Where(p => p.Tags.Count > 0)
                .SelectMany(p => p.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(t => new Tag { Name = t })
                .OrderBy(t => t.Name)
                .ToList();
        }
    }
}