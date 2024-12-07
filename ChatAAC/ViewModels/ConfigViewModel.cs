using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ChatAAC.Models;
using OllamaSharp;
using ReactiveUI;

namespace ChatAAC.ViewModels;

public class ConfigViewModel : ReactiveObject
{
    #region Constructor

    private ConfigViewModel()
    {
        EnsureConfigDirectoryExists();
        LoadConfiguration();
        NormalizeOllamaAddress();

        SaveCommand = ReactiveCommand.Create(SaveConfiguration);
        AddBoardCommand = ReactiveCommand.CreateFromTask(AddBoardPathAsync);
        RemoveBoardCommand = ReactiveCommand.Create<string>(RemoveBoardPath);

        // Subskrybuj zmiany w kolekcji BoardPaths
        BoardPaths.CollectionChanged += (_, _) =>
        {
            _isDirty = true;
            SaveConfiguration();
        };

        _ = InitializeModelsAsync();
    }

    #endregion

    #region Singleton Implementation

    private static readonly Lazy<ConfigViewModel> _instance =
        new(() => new ConfigViewModel(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static ConfigViewModel Instance => _instance.Value;

    private string? _message;

    public string? Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    #endregion

    #region Fields

    private string _ollamaAddress = "http://localhost:11434";
    private string _selectedModel = "gemma2";
    private bool _showSex;
    private bool _showViolence;
    private bool _showAac;
    private bool _showSchematic;
    private string? _selectedLanguage;
    private int _loadedIconsCount;

    private string _defaultBoardPath = string.Empty;

    #endregion

    #region Configuration Storage

    private readonly string _configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC",
        "config.json");

    private readonly object _saveLock = new();
    private bool _isDirty;

    #endregion

    #region Properties

    public string OllamaAddress
    {
        get => _ollamaAddress;
        set => SetAndSave(ref _ollamaAddress, value);
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set => SetAndSave(ref _selectedModel, value);
    }

    public bool ShowSex
    {
        get => _showSex;
        set => SetAndSave(ref _showSex, value);
    }

    public bool ShowViolence
    {
        get => _showViolence;
        set => SetAndSave(ref _showViolence, value);
    }

    public bool ShowAac
    {
        get => _showAac;
        set => SetAndSave(ref _showAac, value);
    }

    public bool ShowSchematic
    {
        get => _showSchematic;
        set => SetAndSave(ref _showSchematic, value);
    }

    public string? SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetAndSave(ref _selectedLanguage, value);
    }

    public int LoadedIconsCount
    {
        get => _loadedIconsCount;
        set => SetAndSave(ref _loadedIconsCount, value);
    }

    public string DefaultBoardPath
    {
        get => _defaultBoardPath;
        set => SetAndSave(ref _defaultBoardPath, value);
    }

    [JsonIgnore] public ObservableCollection<string> Models { get; } = new();

    [JsonIgnore] public ObservableCollection<string> Languages { get; } = new() { "Polski", "English" };

    [JsonIgnore] public ObservableCollection<string> BoardPaths { get; } = new();

    #endregion

    #region Commands

    [JsonIgnore] public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    [JsonIgnore] public ICommand AddBoardCommand { get; }

    [JsonIgnore] public ICommand RemoveBoardCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    ///     Adds a board path to the list of board paths.
    /// </summary>
    private async Task AddBoardPathAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow?.StorageProvider == null) return;

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Wybierz plik tablicy AAC",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Pliki OBZ lub OBF")
                    {
                        Patterns = new[] { "*.obz", "*.obf" }
                    }
                }
            });

            var file = files.FirstOrDefault();
            if (file != null)
            {
                var path = file.Path.LocalPath;
                if (!BoardPaths.Contains(path)) BoardPaths.Add(path);
            }
        }
    }

    /// <summary>
    ///     Removes a board path from the list of board paths.
    /// </summary>
    private void RemoveBoardPath(string path)
    {
        if (BoardPaths.Contains(path)) BoardPaths.Remove(path);
    }

    /// <summary>
    ///     Normalizes the Ollama address to ensure it has the correct format.
    /// </summary>
    private void NormalizeOllamaAddress()
    {
        if (!OllamaAddress.StartsWith("http"))
            OllamaAddress = "http://" + OllamaAddress;

        if (!OllamaAddress.Contains(':', StringComparison.Ordinal))
            OllamaAddress += ":11434";
    }

    /// <summary>
    ///     Initializes the list of models by fetching them from the Ollama API.
    /// </summary>
    private async Task InitializeModelsAsync()
    {
        try
        {
            var ollamaClient = new OllamaApiClient(OllamaAddress);
            var models = await ollamaClient.ListLocalModels();
            var sortedModels = models.OrderBy(m => m.Name);

            foreach (var model in sortedModels) Models.Add(model.Name);
        }
        catch (Exception ex)
        {
            // Handle or log the exception
            Console.WriteLine($"Error initializing models: {ex.Message}");
        }
    }

    private void EnsureConfigDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
    }

    /// <summary>
    ///     Loads the configuration from the config file.
    /// </summary>
    private void LoadConfiguration()
    {
        if (!File.Exists(_configFilePath))
        {
            // Inicjalizacja domyślnych wartości
            InitializeDefaults();
            SaveConfiguration();
            return;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var config = JsonSerializer.Deserialize<ConfigData>(json, options);
            if (config == null) return;

            // Aktualizacja wszystkich właściwości
            UpdatePropertiesFromConfig(config);
            _isDirty = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            InitializeDefaults();
        }
    }


    private void InitializeDefaults()
    {
        OllamaAddress = "http://localhost:11434";
        SelectedModel = "gemma2";
        SelectedLanguage = "Polski";
        DefaultBoardPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatAAC",
            "communikate-20.obz");
        BoardPaths.Clear();
    }

    private void UpdatePropertiesFromConfig(ConfigData config)
    {
        OllamaAddress = config.OllamaAddress;
        SelectedModel = config.SelectedModel;
        ShowSex = config.ShowSex;
        ShowViolence = config.ShowViolence;
        ShowAac = config.ShowAac;
        ShowSchematic = config.ShowSchematic;
        SelectedLanguage = config.SelectedLanguage ?? "Polski";
        LoadedIconsCount = config.LoadedIconsCount;
        DefaultBoardPath = config.DefaultBoardPath;

        BoardPaths.Clear();
        if (config.BoardPaths == null) return;
        foreach (var path in config.BoardPaths.Where(p => !string.IsNullOrEmpty(p))) BoardPaths.Add(path);
    }

    /// <summary>
    ///     Saves the current configuration to the config file.
    /// </summary>
    private void SaveConfiguration()
    {
        if (!_isDirty) return;

        lock (_saveLock)
        {
            try
            {
                var configData = new ConfigData
                {
                    OllamaAddress = OllamaAddress,
                    SelectedModel = SelectedModel,
                    ShowSex = ShowSex,
                    ShowViolence = ShowViolence,
                    ShowAac = ShowAac,
                    ShowSchematic = ShowSchematic,
                    SelectedLanguage = SelectedLanguage,
                    LoadedIconsCount = LoadedIconsCount,
                    DefaultBoardPath = DefaultBoardPath,
                    BoardPaths = BoardPaths.ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(configData, options);
                File.WriteAllText(_configFilePath, json);
                _isDirty = false;

                Console.WriteLine("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     Helper method to set a property value and save configuration.
    /// </summary>
    private void SetAndSave<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        this.RaiseAndSetIfChanged(ref field, value, propertyName);
        _isDirty = true;
        SaveConfiguration();
    }

    /// <summary>
    ///     Updates the count of loaded icons.
    /// </summary>
    public void UpdateLoadedIconsCount(int count)
    {
        LoadedIconsCount = count;
    }

    /// <summary>
    ///     Reloads settings from the configuration file.
    /// </summary>
    public void ReloadSettings()
    {
        LoadConfiguration();
    }

    #endregion
}