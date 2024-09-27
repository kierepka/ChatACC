using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Text.Json.Serialization;
using OllamaSharp;
using ReactiveUI;
using System.Threading.Tasks;

namespace ChatAAC.ViewModels;

public class ConfigViewModel : ReactiveObject
{
    private static ConfigViewModel? _instance;
    private static readonly object Lock = new object();

    private string _ollamaAddress = "http://localhost:11434";
    private string? _selectedModel;
    private bool _showSex;
    private bool _showViolence;
    private bool _showAac;
    private bool _showSchematic;
    private string? _selectedLanguage;
    private int _loadedIconsCount;

    [JsonIgnore]
    public ObservableCollection<string> Models { get; } = new ObservableCollection<string>();
    [JsonIgnore]
    public ObservableCollection<string> Languages { get; } = new ObservableCollection<string>();
    [JsonIgnore]
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    private const string ConfigFilePath = "config.json";

    // Properties
    public string OllamaAddress
    {
        get => _ollamaAddress;
        set => this.RaiseAndSetIfChanged(ref _ollamaAddress, value);
    }

    public string? SelectedModel
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
            if (_instance == null)
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new ConfigViewModel();
                    }
                }
            }
            return _instance;
        }
    }

    public ConfigViewModel()
    {
        LoadConfiguration();
        NormalizeOllamaAddress();
        SaveCommand = ReactiveCommand.Create(SaveConfiguration);
        InitializeData();
    }

    private void NormalizeOllamaAddress()
    {
        if (!OllamaAddress.StartsWith("http"))
            OllamaAddress = "http://" + OllamaAddress;

        if (OllamaAddress.IndexOf(':', 5) < 0)
            OllamaAddress += ":11434";
    }

    private void InitializeData()
    {
        Languages.Add("Polski");
        Languages.Add("English");
        
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
        if (File.Exists(ConfigFilePath))
        {
            var json = File.ReadAllText(ConfigFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var config = JsonSerializer.Deserialize<ConfigData>(json, options);
            if (config != null)
            {
                OllamaAddress = config.OllamaAddress;
                SelectedModel = config.SelectedModel;
                ShowSex = config.ShowSex;
                ShowViolence = config.ShowViolence;
                ShowAac = config.ShowAac;
                ShowSchematic = config.ShowSchematic;
                SelectedLanguage = config.SelectedLanguage;
                LoadedIconsCount = config.LoadedIconsCount;
            }
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
            LoadedIconsCount = LoadedIconsCount
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(configData, options);
        File.WriteAllText(ConfigFilePath, json);
    }

    public void UpdateLoadedIconsCount(int count)
    {
        LoadedIconsCount = count;
        SaveConfiguration();
    }
}

public class ConfigData
{
    public string OllamaAddress { get; set; } = "";
    public string? SelectedModel { get; set; }
    public bool ShowSex { get; set; }
    public bool ShowViolence { get; set; }
    public bool ShowAac { get; set; }
    public bool ShowSchematic { get; set; }
    public string? SelectedLanguage { get; set; }
    public int LoadedIconsCount { get; set; }
}