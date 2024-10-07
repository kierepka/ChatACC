using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using OllamaSharp;
using ReactiveUI;

namespace ChatAAC.ViewModels
{
    public class ConfigViewModel : ReactiveObject
    {
        private static ConfigViewModel? _instance;
        private static readonly object Lock = new();

        private string _ollamaAddress = "http://localhost:11434";
        private string _selectedModel = string.Empty;
        private bool _showSex;
        private bool _showViolence;
        private bool _showAac;
        private bool _showSchematic;
        private string? _selectedLanguage;
        private int _loadedIconsCount;

        [JsonIgnore]
        public ObservableCollection<string> Models { get; } = new();
        [JsonIgnore]
        public ObservableCollection<string> Languages { get; } = new();
        [JsonIgnore]
        public ObservableCollection<string> BoardPaths { get; } = new();
        [JsonIgnore]
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        [JsonIgnore]
        public ICommand AddBoardCommand { get; }
        [JsonIgnore]
        public ICommand RemoveBoardCommand { get; }

        private const string ConfigFilePath = "config.json";

        // Properties
        public string OllamaAddress
        {
            get => _ollamaAddress;
            set => this.RaiseAndSetIfChanged(ref _ollamaAddress, value);
        }

        public string SelectedModel
        {
            get => _selectedModel;
            set => this.RaiseAndSetIfChanged(ref _selectedModel, value);
        }

        public bool ShowSex
        {
            get => _showSex;
            set => this.RaiseAndSetIfChanged(ref _showSex, value);
        }

        public bool ShowViolence
        {
            get => _showViolence;
            set => this.RaiseAndSetIfChanged(ref _showViolence, value);
        }

        public bool ShowAac
        {
            get => _showAac;
            set => this.RaiseAndSetIfChanged(ref _showAac, value);
        }

        public bool ShowSchematic
        {
            get => _showSchematic;
            set => this.RaiseAndSetIfChanged(ref _showSchematic, value);
        }

        public string? SelectedLanguage
        {
            get => _selectedLanguage;
            set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
        }

        public int LoadedIconsCount
        {
            get => _loadedIconsCount;
            set => this.RaiseAndSetIfChanged(ref _loadedIconsCount, value);
        }

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

        public ConfigViewModel()
        {
            LoadConfiguration();
            NormalizeOllamaAddress();
            SaveCommand = ReactiveCommand.Create(SaveConfiguration);
            AddBoardCommand = ReactiveCommand.CreateFromTask(AddBoardPathAsync);
            RemoveBoardCommand = ReactiveCommand.Create<string>(RemoveBoardPath);
            InitializeData();

            if (string.IsNullOrEmpty(_selectedModel))
                _selectedModel = "gemma2";
        }

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
                        new FilePickerFileType("Pliki OBZ")
                        {
                            Patterns = new[] { "*.obz" }
                        }
                    }
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

        private void RemoveBoardPath(string path)
        {
            if (BoardPaths.Contains(path))
            {
                BoardPaths.Remove(path);
            }
        }

        private void NormalizeOllamaAddress()
        {
            if (!OllamaAddress.StartsWith("http"))
                OllamaAddress = "http://" + OllamaAddress;

            if (!OllamaAddress.Contains(':', StringComparison.Ordinal))
                OllamaAddress += ":11434";
        }

        private void InitializeData()
        {
            Languages.Add("Polski");
            Languages.Add("English");

            if (string.IsNullOrEmpty(DefaultBoardPath))
            {
                DefaultBoardPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatAAC", "communikate-20.obz");
            }

            Task.Run(InitializeModelsAsync);
        }

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

        private void LoadConfiguration()
        {
            if (!File.Exists(ConfigFilePath)) return;
            var json = File.ReadAllText(ConfigFilePath);
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
            SelectedLanguage = config.SelectedLanguage;
            LoadedIconsCount = config.LoadedIconsCount;
            DefaultBoardPath = config.DefaultBoardPath;

            BoardPaths.Clear();
            foreach (var path in config.BoardPaths)
            {
                BoardPaths.Add(path);
            }
        }

        private void SaveConfiguration()
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
            File.WriteAllText(ConfigFilePath, json);
        }

        public string DefaultBoardPath { get; set; } = string.Empty;

        public void UpdateLoadedIconsCount(int count)
        {
            LoadedIconsCount = count;
            SaveConfiguration();
        }
    }

    public class ConfigData
    {
        public string OllamaAddress { get; set; } = string.Empty;
        public string SelectedModel { get; set; } = string.Empty;
        public bool ShowSex { get; set; }
        public bool ShowViolence { get; set; }
        public bool ShowAac { get; set; }
        public bool ShowSchematic { get; set; }
        public string? SelectedLanguage { get; set; }
        public int LoadedIconsCount { get; set; }
        public string DefaultBoardPath { get; set; } = string.Empty;
        public List<string> BoardPaths { get; set; } = new();
    }
}