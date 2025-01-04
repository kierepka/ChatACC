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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models;
using ChatAAC.Views;
using OllamaSharp;
using ReactiveUI;

namespace ChatAAC.ViewModels
{
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
            ClearCacheCommand = ReactiveCommand.Create(ClearCache);
            ExportSettingsCommand = ReactiveCommand.Create(ExportSettings);
            ImportSettingsCommand = ReactiveCommand.Create(ImportSettings);
            OpenAboutWindowCommand = ReactiveCommand.Create(OpenAboutWindow);

            // Monitor changes in the BoardPaths collection
            BoardPaths.CollectionChanged += (_, _) =>
            {
                _isDirty = true;
                SaveConfiguration();
            };


            _ = InitializeModelsAsync();
        }

        #endregion

        #region Singleton Implementation

        private static readonly Lazy<ConfigViewModel> LazyInstance =
            new(() => new ConfigViewModel(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ConfigViewModel Instance => LazyInstance.Value;

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
        private string? _selectedLanguage;
        private string _defaultBoardPath = string.Empty;

        #endregion

        #region Configuration Storage

        private readonly string _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatAAC",
            "config.json");

        private readonly Lock _saveLock = new();
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

        public string? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                SetAndSave(ref _selectedLanguage, value);
                ChangeLanguage(value);
            }
        }
        
        public CultureInfo? SelectedCulture { get; set; }
        
        private void ChangeLanguage(string? language)
        {
            if (string.IsNullOrEmpty(language)) return;

            SelectedCulture = language switch
            {
                "Polski" => new CultureInfo("pl-PL"),
                "English" => new CultureInfo("en-US"),
                _ => CultureInfo.InvariantCulture
            };

            Thread.CurrentThread.CurrentCulture = SelectedCulture;
            Thread.CurrentThread.CurrentUICulture = SelectedCulture;

            Resources.Culture = SelectedCulture;
        }

        public string DefaultBoardPath
        {
            get => _defaultBoardPath;
            set => SetAndSave(ref _defaultBoardPath, value);
        }

        public string CacheDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatAAC", "Cache");

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
            get => _fontSizeSmall;
            set => SetAndSave(ref _fontSizeBig, value);
        }

        public double FontSizeLarge
        {
            get => _fontSizeLarge;
            set => SetAndSave(ref _fontSizeLarge, value);
        }


        public double ButtonSize
        {
            get => _buttonSize;
            set => SetAndSave(ref _buttonSize, value);
        }

        [JsonIgnore] public ObservableCollection<string> Models { get; } = [];

        [JsonIgnore] public ObservableCollection<string> Languages { get; } = ["Polski", "English"];

        [JsonIgnore] public ObservableCollection<string> BoardPaths { get; } = [];

        #endregion

        #region Commands

        [JsonIgnore] public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        [JsonIgnore] public ICommand AddBoardCommand { get; }

        [JsonIgnore] public ICommand RemoveBoardCommand { get; }

        [JsonIgnore] public ICommand OpenAboutWindowCommand { get; }

        [JsonIgnore] public ICommand ExportSettingsCommand { get; }
        [JsonIgnore] public ICommand ImportSettingsCommand { get; }

        [JsonIgnore] public ICommand ClearCacheCommand { get; }

        #endregion

        #region Methods

        private void ExportSettings()
        {
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

        private void OpenAboutWindow()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime) return;
            var aboutWindow = new AboutWindow
            {
                DataContext = new AboutViewModel()
            };
            aboutWindow.Show();
        }

        private void ClearCache()
        {
            if (!Directory.Exists(CacheDirectory)) return;
            Directory.Delete(CacheDirectory, true);
            Directory.CreateDirectory(CacheDirectory);
        }

        public void ReloadSettings()
        {
            LoadConfiguration();
        }

        private async Task AddBoardPathAsync()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
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
                    if (!BoardPaths.Contains(path)) BoardPaths.Add(path);
                }
            }
        }

        private void RemoveBoardPath(string path)
        {
            if (BoardPaths.Contains(path)) BoardPaths.Remove(path);
        }

        private void NormalizeOllamaAddress()
        {
            if (!OllamaAddress.StartsWith("http"))
                OllamaAddress = "http://" + OllamaAddress;

            if (!OllamaAddress.Contains(':'))
                OllamaAddress += ":11434";
        }

        private async Task InitializeModelsAsync()
        {
            try
            {
                var ollamaClient = new OllamaApiClient(OllamaAddress);
                var models = await ollamaClient.ListLocalModelsAsync();
                var sortedModels = models.OrderBy(m => m.Name);

                foreach (var model in sortedModels) Models.Add(model.Name);
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
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
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

                if (config != null) UpdatePropertiesFromConfig(config);
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
            SelectedLanguage = "Polski";
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

        private void SetAndSave<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            this.RaiseAndSetIfChanged(ref field, value, propertyName);
            _isDirty = true;
            SaveConfiguration();
        }

        #endregion
    }
}