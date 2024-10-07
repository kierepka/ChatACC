using Avalonia.Threading;
using ChatAAC.Models;
using ChatAAC.Services;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using ChatAAC.Models.Obf;
using ChatAAC.Views;
using Button = ChatAAC.Models.Obf.Button;

namespace ChatAAC.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<Button> Buttons { get; set; } = new();
    public ObservableCollection<Button> SelectedButtons { get; set; } = new();

    private string _selectedTense = "Teraźniejszy";

    public string SelectedTense
    {
        get => _selectedTense;
        set => this.RaiseAndSetIfChanged(ref _selectedTense, value);
    }

    private string _selectedForm = "Oznajmująca";

    public string SelectedForm
    {
        get => _selectedForm;
        set => this.RaiseAndSetIfChanged(ref _selectedForm, value);
    }

    private int _quantity = 1;

    public int Quantity
    {
        get => _quantity;
        set => this.RaiseAndSetIfChanged(ref _quantity, value);
    }

    private bool _isConfigBarVisible;

    public bool IsConfigBarVisible
    {
        get => _isConfigBarVisible;
        set => this.RaiseAndSetIfChanged(ref _isConfigBarVisible, value);
    }

    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadMainBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSelectedCommand { get; }
    public ReactiveCommand<Button, Unit> ButtonClickedCommand { get; }
    public ReactiveCommand<Button, Unit> RemoveButtonCommand { get; }
    public ReactiveCommand<Unit, Unit> SpeakCommand { get; }
    public ReactiveCommand<Unit, Unit> SendToAiCommand { get; }
    public ReactiveCommand<Unit, Unit> SpeakAiResponseCommand { get; }
    public ReactiveCommand<Unit, Unit> CopySentenceCommand { get; }
    public ReactiveCommand<string, Unit> CopyHistoryItemCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearHistoryCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyAiResponseCommand { get; }

    private readonly OllamaClient _ollamaClient = new();
    private readonly ITtsService _ttsService;

    public ObservableCollection<AiResponse> AiResponseHistory { get; }

    private string _constructedSentence = string.Empty;

    public string ConstructedSentence
    {
        get => _constructedSentence;
        private set => this.RaiseAndSetIfChanged(ref _constructedSentence, value);
    }

    private string _aiResponse = string.Empty;

    public string AiResponse
    {
        get => _aiResponse;
        set => this.RaiseAndSetIfChanged(ref _aiResponse, value);
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private bool _isFullScreen = false;

    public bool IsFullScreen
    {
        get => _isFullScreen;
        set => this.RaiseAndSetIfChanged(ref _isFullScreen, value);
    }

    private ObfFile? _obfData;

    public ObfFile? ObfData
    {
        get => _obfData;
        set => this.RaiseAndSetIfChanged(ref _obfData, value);
    }

    // Bufor dla wczytanych plików OBF
    private readonly ConcurrentDictionary<string, ObfFile> _obfCache = new();

    // Nowe właściwości
    private int _currentBoardIndex = 0;

    public int CurrentBoardIndex
    {
        get => _currentBoardIndex;
        set => this.RaiseAndSetIfChanged(ref _currentBoardIndex, value);
    }

    public ReactiveCommand<Unit, Unit> NextBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousBoardCommand { get; }

    public MainViewModel()
    {
        AiResponseHistory = new();
        LoadHistory();

        // Inicjalizacja TTS w zależności od platformy
        _ttsService = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MacTtsService()
            : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsTtsService()
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? new LinuxTtsService()
            : throw new PlatformNotSupportedException("Platforma nie jest wspierana przez TTS.");

        LoadMainBoardCommand = ReactiveCommand.CreateFromTask(LoadInitialFile);
        ClearSelectedCommand = ReactiveCommand.Create(ClearSelected);
        ButtonClickedCommand = ReactiveCommand.CreateFromTask<Button>(OnButtonClickedAsync);
        RemoveButtonCommand = ReactiveCommand.Create<Button>(OnRemoveButton);
        SpeakCommand = ReactiveCommand.Create(OnSpeak);
        OpenSettingsCommand = ReactiveCommand.Create(OnOpenSettings);

        SendToAiCommand = ReactiveCommand.CreateFromTask(OnSendToAiAsync, outputScheduler: RxApp.MainThreadScheduler);
        SpeakAiResponseCommand =
            ReactiveCommand.CreateFromTask(OnSpeakAiResponseAsync, outputScheduler: RxApp.MainThreadScheduler);
        CopySentenceCommand = ReactiveCommand.Create(OnCopySentence);
        CopyHistoryItemCommand = ReactiveCommand.Create<string>(CopyToClipboard);
        ClearHistoryCommand = ReactiveCommand.Create(ClearHistory);
        CopyAiResponseCommand = ReactiveCommand.Create(OnCopyAiResponse);

        NextBoardCommand = ReactiveCommand.CreateFromTask(LoadNextBoardAsync);
        PreviousBoardCommand = ReactiveCommand.CreateFromTask(LoadPreviousBoardAsync);

        SelectedButtons.CollectionChanged += (_, _) => UpdateConstructedSentence();

        _ = LoadInitialFile();
    }

    private bool _isInitialized;

    private async Task LoadNextBoardAsync()
    {
        var boardPaths = ConfigViewModel.Instance.BoardPaths;
        if (boardPaths.Count == 0)
        {
            Console.WriteLine("Brak dodatkowych tablic do załadowania.");
            return;
        }

        CurrentBoardIndex = (CurrentBoardIndex + 1) % boardPaths.Count;
        var nextBoardPath = boardPaths[CurrentBoardIndex];
        await LoadObfOrObzFileAsync(nextBoardPath);
    }

    private async Task LoadPreviousBoardAsync()
    {
        var boardPaths = ConfigViewModel.Instance.BoardPaths;
        if (boardPaths.Count == 0)
        {
            Console.WriteLine("Brak dodatkowych tablic do załadowania.");
            return;
        }

        CurrentBoardIndex = (CurrentBoardIndex - 1 + boardPaths.Count) % boardPaths.Count;
        var previousBoardPath = boardPaths[CurrentBoardIndex];
        await LoadObfOrObzFileAsync(previousBoardPath);
    }

    private async Task LoadInitialFile()
    {
        if (_isInitialized || string.IsNullOrEmpty(ConfigViewModel.Instance.DefaultBoardPath))
            return;

        IsLoading = true;
        try
        {
            var defaultBoardPath = ConfigViewModel.Instance.DefaultBoardPath;

            if (!string.IsNullOrEmpty(defaultBoardPath))
            {
                await LoadObfOrObzFileAsync(defaultBoardPath);
            }
            else if (ConfigViewModel.Instance.BoardPaths.Count > 0)
            {
                await LoadObfOrObzFileAsync(ConfigViewModel.Instance.BoardPaths[0]);
            }
            else
            {
                Console.WriteLine("Brak domyślnej tablicy lub dodatkowych tablic do załadowania.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wczytywania pliku początkowego: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _isInitialized = true;
        }
    }

    private async Task LoadObfOrObzFileAsync(string filePath)
    {
        if (filePath.EndsWith(".obz"))
            await LoadObzFileAsync(filePath);
        else if (filePath.EndsWith(".obf"))
            await LoadObfFileAsync(filePath);
        else
            Console.WriteLine("Nieobsługiwany typ pliku. Podaj plik z rozszerzeniem .obf lub .obz.");
    }

    private async Task LoadObfFileAsync(string filePath)
    {
        try
        {
            IsLoading = true;
            Console.WriteLine($"Wczytywanie pliku OBF: {filePath}");

            if (_obfCache.TryGetValue(filePath, out var cachedObf))
            {
                Console.WriteLine($"Plik OBF załadowany z bufora: {filePath}");
                ObfData = cachedObf;
            }
            else
            {
                var obfData = await ObfLoader.LoadObfAsync(filePath);
                if (obfData != null)
                {
                    _obfCache[filePath] = obfData;
                    ObfData = obfData;
                    Console.WriteLine("Plik OBF został wczytany i dodany do bufora.");
                }
                else
                {
                    Console.WriteLine("Plik OBF jest pusty lub niepoprawny.");
                    return;
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Buttons.Clear();
                foreach (var button in ObfData.Buttons)
                {
                    Buttons.Add(button);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wczytywania pliku OBF: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadObzFileAsync(string filePath)
    {
        IsLoading = true;
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(tempDirectory);

            using var archive = ZipFile.OpenRead(filePath);
            foreach (var entry in archive.Entries)
            {
                // Bezpieczna ekstrakcja każdego wpisu
                var destinationPath = Path.GetFullPath(Path.Combine(tempDirectory, entry.FullName));

                if (!destinationPath.StartsWith(tempDirectory, StringComparison.Ordinal))
                {
                    // Ostrzeżenie lub pominięcie niebezpiecznego wpisu
                    Console.WriteLine($"Pominięto niebezpieczny wpis: {entry.FullName}");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    // To jest katalog, utwórz go
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    // Upewnij się, że katalog docelowy istnieje
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            var manifestPath = Path.Combine(tempDirectory, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson);

                var rootObfPath = Path.Combine(tempDirectory, manifest?.Root ?? "root.obf");
                if (File.Exists(rootObfPath))
                {
                    await LoadObfFileAsync(rootObfPath);
                }
                else
                {
                    Console.WriteLine("Plik root.obf nie został znaleziony w paczce OBZ.");
                }
            }
            else
            {
                Console.WriteLine("Plik manifest.json nie został znaleziony w paczce OBZ.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wczytywania pliku OBZ: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            // Usuwamy tymczasowy katalog po użyciu
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    private void OnOpenSettings()
    {
        var configWindow = new ConfigWindow
        {
            DataContext = new ConfigViewModel()
        };
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
        if (mainWindow != null)
            configWindow.ShowDialog(mainWindow);
        else
            configWindow.Show();
    }

    private void ClearSelected() => SelectedButtons.Clear();

    private async Task OnButtonClickedAsync(Button button)
    {
        Console.WriteLine($"Kliknięto przycisk: {button.Label}");

        if (button.LoadBoard != null)
        {
            var boardPath = Path.Combine(
                ObfLoader.ObfCacheDirectory,
                button.LoadBoard.Path);


            if (_obfCache.TryGetValue(boardPath, out ObfFile? value))
            {
                Console.WriteLine($"Ładowanie tablicy z bufora: {boardPath}");
                ObfData = value;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Buttons.Clear();
                    foreach (var btn in ObfData.Buttons)
                    {
                        Buttons.Add(btn);
                    }
                });
            }
            else
            {
                await LoadObfOrObzFileAsync(boardPath);
            }
        }
        else
        {
            if (SelectedButtons.Contains(button)) return;
            SelectedButtons.Add(button);
            UpdateConstructedSentence();
        }
    }

    private void OnRemoveButton(Button button)
    {
        if (SelectedButtons.Contains(button))
        {
            SelectedButtons.Remove(button);
            UpdateConstructedSentence();
        }
    }

    private void UpdateConstructedSentence()
    {
        ConstructedSentence = string.Join(" ", SelectedButtons.Select(p => p.Label));
        Console.WriteLine($"Skonstruowane zdanie: {ConstructedSentence}");
    }

    private void OnSpeak()
    {
        var sentence = ConstructedSentence;
        Task.Run(async () =>
        {
            try
            {
                await _ttsService.SpeakAsync(sentence).ConfigureAwait(false);
                Console.WriteLine($"Odczytano zdanie: {sentence}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odczytywania tekstu: {ex.Message}");
            }
        });
    }

    private async Task OnSendToAiAsync()
    {
        if (string.IsNullOrWhiteSpace(ConstructedSentence))
        {
            AiResponse = "Nie wybrano żadnych przycisków.";
            return;
        }

        try
        {
            IsLoading = true;
            AiResponse = "Generowanie odpowiedzi...";
            Console.WriteLine($"Wysyłanie zapytania: {ConstructedSentence}");

            var chatRequest = new ChatRequest
            {
                Model = ConfigViewModel.Instance.SelectedModel,
                Prompt = ConstructedSentence,
                Form = SelectedForm,
                Tense = SelectedTense,
                Quantity = Quantity
            };

            var response = await _ollamaClient.ChatAsync(chatRequest).ConfigureAwait(false);
            AiResponse = await CombineAsyncEnumerableAsync(response).ConfigureAwait(false);

            Console.WriteLine($"Odpowiedź AI: {AiResponse}");
        }
        catch (Exception ex)
        {
            AiResponse = $"Błąd: {ex.Message}";
            Console.WriteLine($"Błąd podczas komunikacji z AI: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            AddAiResponseToHistory(AiResponse);
            OnCopyAiResponse();
            await OnSpeakAiResponseAsync();
        }
    }

    private async Task<string> CombineAsyncEnumerableAsync(IAsyncEnumerable<string> asyncStrings)
    {
        var stringBuilder = new StringBuilder();
        await foreach (var str in asyncStrings.ConfigureAwait(false))
        {
            stringBuilder.Append(str);
        }

        return stringBuilder.ToString();
    }

    private async Task OnSpeakAiResponseAsync()
    {
        if (!string.IsNullOrEmpty(AiResponse))
        {
            try
            {
                await _ttsService.SpeakAsync(AiResponse).ConfigureAwait(false);
                Console.WriteLine($"Odczytano odpowiedź AI: {AiResponse}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odczytywania odpowiedzi AI: {ex.Message}");
            }
        }
    }

    private void OnCopyAiResponse()
    {
        if (!string.IsNullOrEmpty(AiResponse))
        {
            CopyToClipboard(AiResponse);
        }
    }

    private void OnCopySentence()
    {
        if (!string.IsNullOrEmpty(ConstructedSentence))
        {
            CopyToClipboard(ConstructedSentence);
        }
    }

    private void CopyToClipboard(string textToClipboard)
    {
        Dispatcher.UIThread.Post(() =>
        {
            switch (Application.Current?.ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }:
                    window.Clipboard?.SetTextAsync(textToClipboard);
                    break;
                case ISingleViewApplicationLifetime { MainView: { } mainView }:
                    if (mainView.GetVisualRoot() is TopLevel topLevel)
                        topLevel.Clipboard?.SetTextAsync(textToClipboard);
                    break;
                default:
                    Console.WriteLine("Clipboard nie jest dostępny.");
                    break;
            }
        });
    }

    private void AddAiResponseToHistory(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return;
        AiResponseHistory.Add(new AiResponse(response));
        SaveHistory();
    }

    private readonly string _historyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC",
        "ai_response_history.json");

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!Directory.Exists(directory) && directory != null)
                Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(AiResponseHistory, options);
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisywania historii: {ex.Message}");
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyFilePath)) return;
            var json = File.ReadAllText(_historyFilePath);
            var history = JsonSerializer.Deserialize<ObservableCollection<AiResponse>>(json);
            if (history == null) return;
            foreach (var response in history)
                AiResponseHistory.Add(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas ładowania historii: {ex.Message}");
        }
    }

    private void ClearHistory()
    {
        AiResponseHistory.Clear();
        SaveHistory();
    }
}