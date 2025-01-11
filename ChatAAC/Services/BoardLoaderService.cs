using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models.Obf;
using ChatAAC.ViewModels;

namespace ChatAAC.Services;

public class BoardLoaderService(MainViewModel viewModel)
{
    private static string ObfCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Obf");

    private static string PictogramsCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Pictograms");

    public async Task LoadObfOrObzFileAsync(string filePath)
    {
        if (filePath.EndsWith(".obz", StringComparison.OrdinalIgnoreCase))
        {
            await LoadObzFileAsync(filePath);
        }
        else if (filePath.EndsWith(".obf", StringComparison.OrdinalIgnoreCase))
        {
            var obfFileName = Path.GetFileName(filePath);
            var cachedObfPath = Path.Combine(ObfCacheDirectory, viewModel.ObzDirectoryName, obfFileName);
            await LoadObfFileAsync(cachedObfPath);
        }
        else
        {
            AppLogger.LogInfo(
                Resources
                    .BoardLoaderService_LoadObfOrObzFileAsync_Nieobsługiwany_typ_pliku__Podaj_plik_z_rozszerzeniem__obf_lub__obz_);
        }
    }

    public async Task LoadObfFileAsync(string? filePath)
    {
        viewModel.IsLoading = true;
        try
        {
            var obfFile = await ObfLoader.LoadObfAsync(filePath);
            if (obfFile == null)
            {
                AppLogger.LogInfo(Resources.BoardLoaderService_LoadObfFileAsync_Plik_OBF_jest_pusty_lub_nieprawidłowy_);
                return;
            }

            viewModel.ObfData = obfFile;
            viewModel.CurrentObfFilePath = filePath ?? string.Empty;

            // If we are loading a file that is not in the history
            if (viewModel.CurrentHistoryIndex == -1
                || viewModel.CurrentHistoryIndex == viewModel.ObfFileHistory.Count - 1
                || viewModel.ObfFileHistory[viewModel.CurrentHistoryIndex] != filePath)
            {
                // Delete element in history after current history index
                if (viewModel.CurrentHistoryIndex < viewModel.ObfFileHistory.Count - 1)
                    viewModel.ObfFileHistory = viewModel.ObfFileHistory
                        .Take(viewModel.CurrentHistoryIndex + 1).ToList();
                // Add new element to history
                viewModel.ObfFileHistory.Add(filePath);
                viewModel.CurrentHistoryIndex = viewModel.ObfFileHistory.Count - 1;
            }

            // Load buttons from OBF file
            LoadButtonsFromObfData(obfFile);
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.BoardLoaderService_LoadObfFileAsync_Błąd_podczas_ładowania_pliku_OBF___0___Szczegóły___1_,
                filePath, ex.Message));
        }
        finally
        {
            viewModel.IsLoading = false;
        }
    }

    private async Task LoadObzFileAsync(string filePath)
    {
        viewModel.IsLoading = true;

        try
        {
            // Create a subdirectory with the name corresponding to the .obz file
            viewModel.ObzDirectoryName = Path.GetFileNameWithoutExtension(filePath);

            var destinationDirectory = Path.Combine(ObfCacheDirectory, viewModel.ObzDirectoryName);

            // Extract the contents of the OBZ file
            await ExtractObzArchiveAsync(filePath);

            var manifestPath = Path.Combine(destinationDirectory, "manifest.json");
            if (File.Exists(manifestPath))
            {
                // Read the manifest file
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson);

                // Construct the path to the root OBF file
                var rootObfPath = Path.Combine(destinationDirectory, Path.GetFileName( manifest?.Root ?? "root.obf"));
                if (File.Exists(rootObfPath))
                    // Load the root OBF file
                    await LoadObfFileAsync(rootObfPath);
                else
                    AppLogger.LogInfo(Resources
                        .BoardLoaderService_LoadObzFileAsync_root_obf_file_not_found_in_the_OBZ_package_);
            }
            else
            {
                AppLogger.LogInfo(Resources
                    .BoardLoaderService_LoadObzFileAsync_manifest_json_file_not_found_in_the_OBZ_package_);
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(Resources.BoardLoaderService_LoadObzFileAsync_Error_loading_OBZ_file___0_,
                ex.Message));
        }
        finally
        {
            viewModel.IsLoading = false;
        }
    }

    private async Task ExtractObzArchiveAsync(string filePath)
    {
        var pictogramsPath = Path.Combine(PictogramsCacheDirectory, viewModel.ObzDirectoryName);
        var obfPath = Path.Combine(ObfCacheDirectory, viewModel.ObzDirectoryName);

        Directory.CreateDirectory(pictogramsPath);
        Directory.CreateDirectory(obfPath);

        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(filePath);
            foreach (var entry in archive.Entries)
            {
                var sanitizedEntryName = SanitizeEntryName(entry.FullName);

                string? destinationDirectory = null;

                if (IsImageFile(sanitizedEntryName))
                    destinationDirectory = pictogramsPath;
                else if (IsObfFile(sanitizedEntryName))
                    destinationDirectory = obfPath;
                else if (IsManifestFile(sanitizedEntryName)) destinationDirectory = obfPath;

                if (destinationDirectory == null)
                    continue;

                var destinationPath =
                    Path.GetFullPath(Path.Combine(destinationDirectory, Path.GetFileName(sanitizedEntryName)));

                if (!destinationPath.StartsWith(destinationDirectory, StringComparison.Ordinal))
                {
                    AppLogger.LogInfo(string.Format(
                        Resources.BoardLoaderService_ExtractObzArchiveAsync_Skipped_unsafe_entry___0_, entry.FullName));
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    try
                    {
                        entry.ExtractToFile(destinationPath, true);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        AppLogger.LogError(string.Format(
                            Resources.BoardLoaderService_ExtractObzArchiveAsync_Access_denied_to_file___0____1_,
                            destinationPath, ex.Message));
                    }
                    catch (IOException ex)
                    {
                        AppLogger.LogError(string.Format(
                            Resources.BoardLoaderService_ExtractObzArchiveAsync_Unable_to_overwrite_file___0____1_,
                            destinationPath, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError(string.Format(
                            Resources
                                .BoardLoaderService_ExtractObzArchiveAsync_An_error_occurred_while_extracting_file___0____1_,
                            entry.FullName, ex.Message));
                    }
                }
            }
        });
    }

    private void LoadButtonsFromObfData(ObfFile obfFile)
    {
        viewModel.Buttons.Clear();
        var buttonDictionary = obfFile.Buttons.ToDictionary(b => b.Id, b => b);

        if (obfFile.Grid != null)
        {
            var rowIndex = 0;
            foreach (var row in obfFile.Grid.Order)
            {
                var columnIndex = 0;
                foreach (var buttonId in row)
                {
                    if (buttonId != null && buttonDictionary.TryGetValue(buttonId, out var button))
                    {
                        var buttonViewModel = new ButtonViewModel(button, rowIndex, columnIndex);
                        viewModel.Buttons.Add(buttonViewModel);
                    }

                    columnIndex++;
                }

                rowIndex++;
            }

            viewModel.GridRows = obfFile.Grid.Rows;
            viewModel.GridColumns = obfFile.Grid.Columns;
        }
        else
        {
            foreach (var buttonViewModel in obfFile.Buttons.Select(button =>
                         new ButtonViewModel(button, 0, 0)))
                viewModel.Buttons.Add(buttonViewModel);

            viewModel.GridRows = 1;
            viewModel.GridColumns = viewModel.Buttons.Count;
        }
    }

    private bool IsManifestFile(string fileName)
    {
        return fileName.ToLower().Equals("manifest.json");
    }

    private bool IsImageFile(string fileName)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        return extensions.Contains(fileExtension);
    }

    private bool IsObfFile(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() == ".obf";
    }

    private string SanitizeEntryName(string entryName)
    {
        entryName = entryName.Replace('\\', '/');
        entryName = MainViewModel.MyRegex().Replace(entryName, "");
        entryName = entryName.TrimStart('/');
        var segments = entryName.Split('/');
        var sanitizedSegments = segments.Where(segment => segment != ".." && segment != ".").ToArray();

        return Path.Combine(sanitizedSegments);
    }
}