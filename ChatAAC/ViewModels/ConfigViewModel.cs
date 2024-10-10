using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ChatAAC.Models;
using OllamaSharp;
using ReactiveUI;

namespace ChatAAC.ViewModels
{
    public class ConfigViewModel : ReactiveObject
    {
        #region Singleton Implementation

        private static ConfigViewModel? _instance;
        private static readonly object Lock = new();

        /// <summary>
        /// Singleton instance of ConfigViewModel.
        /// </summary>
        public static ConfigViewModel Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (Lock)
                {
                    _instance ??= new ConfigViewModel();
                }

                return _instance;
            }
        }
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

        private string _defaultBoardPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChatAAC",
            "communikate-20.obz");

        private readonly string _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatAAC", "config.json");

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

        [JsonIgnore] public ObservableCollection<string> Models { get; } = [];

        [JsonIgnore] public ObservableCollection<string> Languages { get; } = ["Polski", "English"];

        [JsonIgnore] public ObservableCollection<string> BoardPaths { get; } = [];

        #endregion

        #region Commands

        [JsonIgnore] public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        [JsonIgnore] public ICommand AddBoardCommand { get; }

        [JsonIgnore] public ICommand RemoveBoardCommand { get; }

        #endregion

        #region Constructor

        private ConfigViewModel()
        {
            LoadConfiguration();
            NormalizeOllamaAddress();

            // Initialize commands
            SaveCommand = ReactiveCommand.Create(SaveConfiguration);
            AddBoardCommand = ReactiveCommand.CreateFromTask(AddBoardPathAsync);
            RemoveBoardCommand = ReactiveCommand.Create<string>(RemoveBoardPath);

            // Initialize models asynchronously
            _ = InitializeModelsAsync();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a board path to the list of board paths.
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
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Pliki OBZ lub OBF")
                        {
                            Patterns = ["*.obz", "*.obf"]
                        }
                    ]
                });

                var file = files.FirstOrDefault();
                if (file != null)
                {
                    var path = file.Path.LocalPath;
                    if (!BoardPaths.Contains(path))
                    {
                        BoardPaths.Add(path);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a board path from the list of board paths.
        /// </summary>
        private void RemoveBoardPath(string path)
        {
            if (BoardPaths.Contains(path))
            {
                BoardPaths.Remove(path);
            }
        }

        /// <summary>
        /// Normalizes the Ollama address to ensure it has the correct format.
        /// </summary>
        private void NormalizeOllamaAddress()
        {
            if (!OllamaAddress.StartsWith("http"))
                OllamaAddress = "http://" + OllamaAddress;

            if (!OllamaAddress.Contains(':', StringComparison.Ordinal))
                OllamaAddress += ":11434";
        }

        /// <summary>
        /// Initializes the list of models by fetching them from the Ollama API.
        /// </summary>
        private async Task InitializeModelsAsync()
        {
            try
            {
                var ollamaClient = new OllamaApiClient(OllamaAddress);
                var models = await ollamaClient.ListLocalModels();
                var sortedModels = models.OrderBy(m => m.Name);

                foreach (var model in sortedModels)
                {
                    Models.Add(model.Name);
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                Console.WriteLine($"Error initializing models: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the configuration from the config file.
        /// </summary>
        private void LoadConfiguration()
        {
            if (!File.Exists(_configFilePath)) return;
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var config = JsonSerializer.Deserialize<ConfigData>(json, options);
                if (config == null) return;
                OllamaAddress = config.OllamaAddress;
                SelectedModel = config.SelectedModel;
                ShowSex = config.ShowSex;
                ShowViolence = config.ShowViolence;
                ShowAac = config.ShowAac;
                ShowSchematic = config.ShowSchematic;
                SelectedLanguage = config.SelectedLanguage ?? SelectedLanguage;
                LoadedIconsCount = config.LoadedIconsCount;
                DefaultBoardPath = config.DefaultBoardPath;

                BoardPaths.Clear();
                foreach (var path in config.BoardPaths.Where(path => !BoardPaths.Contains(path)))
                {
                    BoardPaths.Add(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current configuration to the config file.
        /// </summary>
        private void SaveConfiguration()
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
                
                // Wywołaj interakcję po zapisaniu konfiguracji
                //CloseWindowInteraction.Handle(Unit.Default).Subscribe();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to set a property value and save configuration.
        /// </summary>
        private void SetAndSave<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            this.RaiseAndSetIfChanged(ref field, value, propertyName);
        }

        /// <summary>
        /// Updates the count of loaded icons.
        /// </summary>
        public void UpdateLoadedIconsCount(int count)
        {
            LoadedIconsCount = count;
        }

        /// <summary>
        /// Reloads settings from the configuration file.
        /// </summary>
        public void ReloadSettings()
        {
            LoadConfiguration();
        }

        #endregion
    }
}