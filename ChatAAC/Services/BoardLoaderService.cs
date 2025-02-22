using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChatAAC.Abstractions.Services;
using ChatAAC.Lang;
using ChatAAC.Models.Obf;
using ChatAAC.ViewModels;
using Microsoft.Extensions.Logging;

namespace ChatAAC.Services;

public partial class BoardLoaderService(
    MainViewModel viewModel,
    ILogger logger,
    IFileTypeValidator fileTypeValidator,
    ICachePathProvider cachePathProvider)
    : IBoardLoaderService
{
    private readonly MainViewModel _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileTypeValidator _fileTypeValidator = fileTypeValidator ?? throw new ArgumentNullException(nameof(fileTypeValidator));
    private readonly ICachePathProvider _cachePathProvider = cachePathProvider ?? throw new ArgumentNullException(nameof(cachePathProvider));

    public async Task LoadObfOrObzFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try 
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (_fileTypeValidator.IsObzFile(filePath))
            {
                await LoadObzFileAsync(filePath, cancellationToken);
            }
            else if (_fileTypeValidator.IsObfFile(filePath))
            {
                var obfFileName = Path.GetFileName(filePath);
                var cachedObfPath = Path.Combine(
                    _cachePathProvider.GetObfCacheDirectory(), 
                    _viewModel.ObzDirectoryName, 
                    obfFileName
                );
                await LoadObfFileAsync(cachedObfPath, cancellationToken);
            }
            else
            {
                _logger.LogWarning(Resources.BoardLoaderService_LoadObfOrObzFileAsync_Nieobsługiwany_typ_pliku__Podaj_plik_z_rozszerzeniem__obf_lub__obz_);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania pliku: {FilePath}", filePath);
            throw;
        }
    }

    public async Task LoadObfFileAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        _viewModel.IsLoading = true;
        try
        {
            var obfFile = await ObfLoader.LoadObfAsync(filePath, cancellationToken);
            if (obfFile == null)
            {
                _logger.LogWarning("Plik OBF jest pusty lub nieprawidłowy");
                return;
            }

            UpdateObfFileHistory(filePath);
                
            _viewModel.ObfData = obfFile;
            _viewModel.CurrentObfFilePath = filePath ?? string.Empty;

            LoadButtonsFromObfData(obfFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania pliku OBF: {FilePath}", filePath);
        }
        finally
        {
            _viewModel.UpdateGridCells();
            _viewModel.IsLoading = false;
        }
    }

    private void UpdateObfFileHistory(string? filePath)
    {
        if (_viewModel.CurrentHistoryIndex == -1 
            || _viewModel.CurrentHistoryIndex == _viewModel.ObfFileHistory.Count - 1
            || _viewModel.ObfFileHistory[_viewModel.CurrentHistoryIndex] != filePath)
        {
            if (_viewModel.CurrentHistoryIndex < _viewModel.ObfFileHistory.Count - 1)
            {
                _viewModel.ObfFileHistory = _viewModel.ObfFileHistory
                    .Take(_viewModel.CurrentHistoryIndex + 1)
                    .ToList();
            }

            _viewModel.ObfFileHistory.Add(filePath);
            _viewModel.CurrentHistoryIndex = _viewModel.ObfFileHistory.Count - 1;
        }
    }

    private async Task LoadObzFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _viewModel.IsLoading = true;
        try
        {
            _viewModel.ObzDirectoryName = Path.GetFileNameWithoutExtension(filePath);
            var destinationDirectory = Path.Combine(_cachePathProvider.GetObfCacheDirectory(), _viewModel.ObzDirectoryName);
                
            await ExtractObzArchiveAsync(filePath, cancellationToken);
                
            var manifestPath = Path.Combine(destinationDirectory, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson);
                    
                var rootObfPath = Path.Combine(destinationDirectory, Path.GetFileName(manifest?.Root ?? "root.obf"));
                    
                if (File.Exists(rootObfPath))
                    await LoadObfFileAsync(rootObfPath, cancellationToken);
                else
                    _logger.LogWarning("Nie znaleziono pliku root.obf w pakiecie OBZ");
            }
            else
            {
                _logger.LogWarning("Nie znaleziono pliku manifest.json w pakiecie OBZ");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania pliku OBZ");
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private async Task ExtractObzArchiveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var pictogramsPath = Path.Combine(_cachePathProvider.GetPictogramsCacheDirectory(), _viewModel.ObzDirectoryName);
        var obfPath = Path.Combine(_cachePathProvider.GetObfCacheDirectory(), _viewModel.ObzDirectoryName);
            
        Directory.CreateDirectory(pictogramsPath);
        Directory.CreateDirectory(obfPath);

        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(filePath);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                    
                var sanitizedEntryName = SanitizeEntryName(entry.FullName);
                var destinationDirectory = DetermineDestinationDirectory(sanitizedEntryName, pictogramsPath, obfPath);
                    
                if (destinationDirectory == null)
                    continue;

                ExtractArchiveEntry(entry, destinationDirectory);
            }
        }, cancellationToken);
    }

    private string? DetermineDestinationDirectory(string sanitizedEntryName, string pictogramsPath, string obfPath)
    {
        if (_fileTypeValidator.IsImageFile(sanitizedEntryName))
            return pictogramsPath;
            
        if (_fileTypeValidator.IsObfFile(sanitizedEntryName) || _fileTypeValidator.IsManifestFile(sanitizedEntryName))
            return obfPath;
            
        return null;
    }

    private void ExtractArchiveEntry(ZipArchiveEntry entry, string destinationDirectory)
    {
        try
        {
            var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, Path.GetFileName(entry.Name)));

            if (!destinationPath.StartsWith(destinationDirectory, StringComparison.Ordinal))
            {
                _logger.LogWarning("Pominięto niebezpieczny wpis: {EntryName}", entry.FullName);
                return;
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wyodrębniania pliku: {EntryName}", entry.FullName);
        }
    }

    private void LoadButtonsFromObfData(ObfFile obfFile)
    {
        _viewModel.GridCells.Clear();
            
        if (obfFile.Grid != null)
        {
            PopulateGridWithGridDefinition(obfFile);
        }
        else
        {
            PopulateGridWithButtons(obfFile);
        }
    }

    private void PopulateGridWithGridDefinition(ObfFile obfFile)
    {
        var rowIndex = 0;
        if (obfFile.Grid != null)
        {
            
            foreach (var row in obfFile.Grid.Order)
            {
                var columnIndex = 0;
                foreach (var buttonId in row)
                {
                    var btn = string.IsNullOrEmpty(buttonId)
                        ? null
                        : obfFile.Buttons.FirstOrDefault(b => b.Id == buttonId);

                    _viewModel.GridCells.Add(new GridCellViewModel(rowIndex, columnIndex, btn, _viewModel));
                    columnIndex++;
                }

                rowIndex++;
            }

            _viewModel.GridRows = obfFile.Grid.Rows;
            _viewModel.GridColumns = obfFile.Grid.Columns;
        }
        else
        {
            _viewModel.GridRows = 1;
            _viewModel.GridColumns = obfFile.Buttons.Count; 
        }
    }

    private void PopulateGridWithButtons(ObfFile obfFile)
    {
        var col = 0;
        foreach (var btn in obfFile.Buttons)
        {
            _viewModel.GridCells.Add(new GridCellViewModel(0, col, btn, _viewModel));
            col++;
        }
            
        _viewModel.GridRows = 1;
        _viewModel.GridColumns = obfFile.Buttons.Count;
    }

    public async Task SaveObfFileAsync(string filePath, ObfFile obfFile, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(obfFile, options);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private string SanitizeEntryName(string entryName)
    {
        entryName = entryName.Replace('\\', '/');
        var regex = MyRegex();
        entryName = regex.Replace(entryName, "");
        entryName = entryName.TrimStart('/');
            
        var sanitizedSegments = entryName
            .Split('/')
            .Where(segment => segment != ".." && segment != ".")
            .ToArray();
            
        return Path.Combine(sanitizedSegments);
    }

    [GeneratedRegex(@"^[a-zA-Z]:/")]
    private static partial Regex MyRegex();
}