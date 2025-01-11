using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models;
using ChatAAC.Views;
using OllamaSharp;
using ReactiveUI;

namespace ChatAAC.ViewModels;

public class ConfigViewModel : ReactiveObject
{
    #region Singleton

    // Lazy singleton to share the same instance across the application
    private static readonly Lazy<ConfigViewModel> LazyInstance =
        new(() => new ConfigViewModel(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static ConfigViewModel Instance => LazyInstance.Value;

    // Private constructor to enforce the singleton pattern
    private ConfigViewModel()
    {
        EnsureConfigDirectoryExists();
        LoadConfiguration();
        NormalizeOllamaAddress();

        // Initialize commands
        SaveCommand = ReactiveCommand.Create(SaveConfiguration);
        AddBoardCommand = ReactiveCommand.CreateFromTask(AddBoardPathAsync);
        RemoveBoardCommand = ReactiveCommand.Create<string>(RemoveBoardPath);
        ClearCacheCommand = ReactiveCommand.Create(ClearCache);
        ExportSettingsCommand = ReactiveCommand.Create(ExportSettings);
        ImportSettingsCommand = ReactiveCommand.Create(ImportSettings);
        OpenAboutWindowCommand = ReactiveCommand.Create(OpenAboutWindow);
        CloseWindowCommand = ReactiveCommand.Create<Window>(OnCloseWindow);

        // Monitor changes in the BoardPaths collection
        BoardPaths.CollectionChanged += (_, _) =>
        {
            _isDirty = true;
            SaveConfiguration();
        };

        // Asynchronously load models for Ollama
        _ = InitializeModelsAsync();
    }

    #endregion

    #region Localized Text Properties

    // These properties replace all x:Static references to lang:Resources in ConfigWindow
    [JsonIgnore] public string SettingsWindowTitle => Resources.SettingsWindowTitle;
    [JsonIgnore] public string SettingsTitle => Resources.SettingsTitle;

    [JsonIgnore] public string GeneralTab => Resources.GeneralTab;
    [JsonIgnore] public string OllamaAddressLabel => Resources.OllamaAddressLabel;
    [JsonIgnore] public string SelectModelLabel => Resources.SelectModelLabel;
    [JsonIgnore] public string ProgramLanguageLabel => Resources.ProgramLanguageLabel;

    [JsonIgnore] public string BoardsTab => Resources.BoardsTab;
    [JsonIgnore] public string ShowSymbolsLabel => Resources.ShowSymbolsLabel;
    [JsonIgnore] public string IncludeSexSymbols => Resources.IncludeSexSymbols;
    [JsonIgnore] public string IncludeViolenceSymbols => Resources.IncludeViolenceSymbols;
    [JsonIgnore] public string DefaultBoardLabel => Resources.DefaultBoardLabel;
    [JsonIgnore] public string AacBoardFilesLabel => Resources.AACBoardFilesLabel;
    [JsonIgnore] public string AddFileButton => Resources.AddFileButton;
    [JsonIgnore] public string BoardPathTooltip => Resources.BoardPathTooltip;

    [JsonIgnore] public string DisplayTab => Resources.DisplayTab;
    [JsonIgnore] public string FontSizeLabel => Resources.FontSizeLabel;
    [JsonIgnore] public string ButtonSizeLabel => Resources.ButtonSizeLabel;

    [JsonIgnore] public string AdvancedTab => Resources.AdvancedTab;
    [JsonIgnore] public string CacheDirectoryLabel => Resources.CacheDirectoryLabel;
    [JsonIgnore] public string ClearCacheButton => Resources.ClearCacheButton;
    [JsonIgnore] public string BackupSettingsLabel => Resources.BackupSettingsLabel;
    [JsonIgnore] public string ExportSettingsButton => Resources.ExportSettingsButton;
    [JsonIgnore] public string ImportSettingsButton => Resources.ImportSettingsButton;

    [JsonIgnore] public string AboutButton => Resources.AboutButton;
    [JsonIgnore] public string SaveButton => Resources.SaveButton;

    [JsonIgnore] public string AboutCloseButton => Resources.AboutCloseButton;

    [JsonIgnore] public string AboutCloseButtonAutomation => Resources.AboutCloseButtonAutomation;

    /// <summary>
    ///     Call this after changing <c>Resources.Culture</c> to update all localized UI text
    ///     in this ViewModel (and any windows bound to it).
    /// </summary>
    public void RefreshLocalizedTexts()
    {
        // Raises PropertyChanged for all properties,
        // prompting the UI to re-bind them from the new culture.
        this.RaisePropertyChanged(string.Empty);
    }

    #endregion

    #region Fields

    private string _ollamaAddress = "http://localhost:11434";
    private string _selectedModel = "gemma2";
    private bool _showSex;
    private bool _showViolence;
    private string? _selectedLanguage;
    private string _defaultBoardPath = string.Empty;

    // Config file location (JSON)
    private readonly string _configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC",
        "config.json");

    private readonly Lock _saveLock = new();
    private bool _isDirty;

    private string? _message;

    #endregion

    #region Properties

    // A message that can be displayed in the UI (optional)
    public string? Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

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

    /// <summary>
    ///     Selected language in the settings (e.g. "Polski" or "English").
    ///     Changing this triggers <see cref="ChangeLanguage" />.
    /// </summary>
    public string? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            SetAndSave(ref _selectedLanguage, value);
            ChangeLanguage(value);
        }
    }

    /// <summary>
    ///     The CultureInfo associated with the chosen language.
    /// </summary>
    public CultureInfo? SelectedCulture { get; set; }

    /// <summary>
    ///     Example method that sets <c>Resources.Culture</c>
    ///     and optionally refreshes the UI windows.
    /// </summary>
    private void ChangeLanguage(string? language)
    {
        if (string.IsNullOrEmpty(language)) return;

        // Map language string to a CultureInfo
        SelectedCulture = language switch
        {
            "Polski" => new CultureInfo("pl-PL"),
            "English" => new CultureInfo("en-US"),
            _ => CultureInfo.InvariantCulture
        };

        // Apply the culture to the current thread
        Thread.CurrentThread.CurrentCulture = SelectedCulture;
        Thread.CurrentThread.CurrentUICulture = SelectedCulture;

        // Update resource dictionary to reflect the new culture
        Resources.Culture = SelectedCulture;

        // Refresh this ViewModel's localized properties
        RefreshLocalizedTexts();

        // (Optional) Refresh other windows in the application
        RefreshAllOpenWindows();
    }

    /// <summary>
    ///     Example approach for refreshing other windows after language change.
    ///     If your app has multiple windows (e.g. MainWindow, HistoryWindow, etc.),
    ///     you can refresh them as needed.
    /// </summary>
    private void RefreshAllOpenWindows()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        // e.g., refresh MainWindow if it has a matching ViewModel
        if (desktop.MainWindow?.DataContext is MainViewModel mainVm)
            mainVm.RefreshLocalizedTexts(); // a similar method in MainViewModel

        // If you maintain references to open windows like HistoryWindow,
        // you could refresh them similarly (assuming they have a method 
        // to re-bind or call RefreshLocalizedTexts()).
    }

    public string DefaultBoardPath
    {
        get => _defaultBoardPath;
        set => SetAndSave(ref _defaultBoardPath, value);
    }

    // Path to the application's cache directory
    public string CacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache");

    // Font-size fields for dynamic UI scaling
    private double _buttonSize = 50.0;
    private double _fontSize = 16.0;
    private double _fontSizeSmall = 14.0;
    private double _fontSizeBig = 18.0;
    private double _fontSizeLarge = 24.0;

    public double FontSize
    {
        get => _fontSize;
        set
        {
            SetAndSave(ref _fontSize, value);
            FontSizeSmall = _fontSize - 2;
            FontSizeBig = _fontSize + 2;
            FontSizeLarge = _fontSize + 8;
        }
    }

    public double FontSizeSmall
    {
        get => _fontSizeSmall;
        set => SetAndSave(ref _fontSizeSmall, value);
    }

    public double FontSizeBig
    {
        get => _fontSizeBig;
        set => SetAndSave(ref _fontSizeBig, value);
    }

    public double FontSizeLarge
    {
        get => _fontSizeLarge;
        set => SetAndSave(ref _fontSizeLarge, value);
    }

    // The size of buttons in the UI
    public double ButtonSize
    {
        get => _buttonSize;
        set => SetAndSave(ref _buttonSize, value);
    }

    // Lists for Models, Languages, BoardPaths
    [JsonIgnore] public ObservableCollection<string> Models { get; } = [];
    [JsonIgnore] public ObservableCollection<string> Languages { get; } = ["Polski", "English"];
    [JsonIgnore] public ObservableCollection<string> BoardPaths { get; } = [];

    #endregion

    #region Commands

    // Commands for saving, adding boards, removing boards, etc.
    [JsonIgnore] public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    [JsonIgnore] public ICommand AddBoardCommand { get; }
    [JsonIgnore] public ICommand RemoveBoardCommand { get; }
    [JsonIgnore] public ICommand OpenAboutWindowCommand { get; }
    [JsonIgnore] public ReactiveCommand<Window, Unit> CloseWindowCommand { get; private set; }
    [JsonIgnore] public ICommand ExportSettingsCommand { get; }
    [JsonIgnore] public ICommand ImportSettingsCommand { get; }
    [JsonIgnore] public ICommand ClearCacheCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    ///     Reloads settings from config.json (useful if the file was changed externally).
    /// </summary>
    public void ReloadSettings()
    {
        LoadConfiguration();
    }

    private async Task AddBoardPathAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = desktop.MainWindow;
        if (mainWindow?.StorageProvider == null) return;

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select AAC Board File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("OBZ/OBF Files") { Patterns = ["*.obz", "*.obf"] }
            ]
        });

        var file = files.FirstOrDefault();
        if (file != null)
        {
            var path = file.Path.LocalPath;
            if (!BoardPaths.Contains(path))
                BoardPaths.Add(path);
        }
    }

    private void RemoveBoardPath(string path)
    {
        if (BoardPaths.Contains(path))
            BoardPaths.Remove(path);
    }

    private void NormalizeOllamaAddress()
    {
        if (!OllamaAddress.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            OllamaAddress = "http://" + OllamaAddress;

        if (!OllamaAddress.Contains(':'))
            OllamaAddress += ":11434";
    }

    private async Task InitializeModelsAsync()
    {
        try
        {
            var ollamaClient = new OllamaApiClient(OllamaAddress);
            var modelsList = await ollamaClient.ListLocalModelsAsync();
            var sortedModels = modelsList.OrderBy(m => m.Name);

            foreach (var model in sortedModels)
                Models.Add(model.Name);
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.ConfigViewModel_InitializeModelsAsync_Error_initializing_models___0_, ex.Message));
        }
    }

    private void EnsureConfigDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    private void LoadConfiguration()
    {
        if (!File.Exists(_configFilePath))
        {
            InitializeDefaults();
            SaveConfiguration();
            return;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<ConfigData>(json);

            if (config != null)
                UpdatePropertiesFromConfig(config);

            _isDirty = false;
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.ConfigViewModel_LoadConfiguration_Error_loading_configuration___0_, ex.Message));
            InitializeDefaults();
        }
    }

    private void InitializeDefaults()
    {
        OllamaAddress = "http://localhost:11434";
        SelectedModel = "gemma2";
        SelectedLanguage = "Polski"; // Default
        FontSize = 16.0;
        ButtonSize = 50.0;
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
        SelectedLanguage = config.SelectedLanguage ?? "Polski";
        DefaultBoardPath = config.DefaultBoardPath;
        FontSize = config.FontSize;
        ButtonSize = config.ButtonSize;

        BoardPaths.Clear();
        if (config.BoardPaths == null) return;
        foreach (var path in config.BoardPaths.Where(p => !string.IsNullOrEmpty(p)))
            BoardPaths.Add(path);
    }

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
                    SelectedLanguage = SelectedLanguage,
                    DefaultBoardPath = DefaultBoardPath,
                    FontSize = FontSize,
                    ButtonSize = ButtonSize,
                    BoardPaths = BoardPaths.ToList()
                };

                var json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
                _isDirty = false;
            }
            catch (Exception ex)
            {
                AppLogger.LogError(string.Format(
                    Resources.ConfigViewModel_SaveConfiguration_Error_saving_configuration___0_, ex.Message));
            }
        }
    }

    private void ExportSettings()
    {
        // Example: copy config.json to the user's desktop
        var destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "config.json");
        File.Copy(_configFilePath, destination, true);
    }

    private void ImportSettings()
    {
        var source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "config.json");
        if (!File.Exists(source)) return;
        File.Copy(source, _configFilePath, true);
        LoadConfiguration();
    }

    private void ClearCache()
    {
        if (!Directory.Exists(CacheDirectory)) return;
        Directory.Delete(CacheDirectory, true);
        Directory.CreateDirectory(CacheDirectory);
    }

    /// <summary>
    ///     Helper to set a property, flag changes, and automatically save.
    /// </summary>
    private void SetAndSave<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        this.RaiseAndSetIfChanged(ref field, value, propertyName);
        _isDirty = true;
        SaveConfiguration();
    }

    private void OnCloseWindow(Window? window)
    {
        window?.Close();
    }

    private void OpenAboutWindow()
    {
        // Show the "About" window if we have a classic desktop environment
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime) return;
        var aboutWindow = new AboutWindow
        {
            DataContext = new AboutViewModel()
        };
        aboutWindow.Show();
    }

    #endregion
}