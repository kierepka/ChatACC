using Avalonia.Threading;
using ChatAAC.Models;
using ChatAAC.Services;
using ReactiveUI;
using System;
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
    public ObservableCollection<Button> Buttons { get; set; } = [];
    public ObservableCollection<Button> SelectedButtons { get; set; } = [];

    // Nowe właściwości dla formy wypowiedzi
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

    // Właściwość dla ukrytego paska konfiguracji
    private bool _isConfigBarVisible;

    public bool IsConfigBarVisible
    {
        get => _isConfigBarVisible;
        set => this.RaiseAndSetIfChanged(ref _isConfigBarVisible, value);
    }

    // Nowe komendy
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }


    private readonly OllamaClient _ollamaClient = new(); // Klient OllamaSharp
    private readonly ITtsService _ttsService; // Interfejs TTS
    private LoadingWindow? _loadingWindow;


    public ObservableCollection<AiResponse> AiResponseHistory { get; }


    private void ExitApplication()
    {
        // Logic to exit the application
        Environment.Exit(0);
    }


    private string _constructedSentence = string.Empty;

    public string ConstructedSentence
    {
        get => _constructedSentence;
        private set => this.RaiseAndSetIfChanged(ref _constructedSentence, value);
    }

    public ReactiveCommand<Button, Unit> ButtonClickedCommand { get; }
    public ReactiveCommand<Button, Unit> RemoveButtonCommand { get; }
    public ReactiveCommand<Unit, Unit> SpeakCommand { get; }
    public ReactiveCommand<Unit, Unit> SendToAiCommand { get; }
    public ReactiveCommand<Unit, Unit> SpeakAiResponseCommand { get; }
    public ReactiveCommand<Unit, Unit> CopySentenceCommand { get; }
    public ReactiveCommand<string, Unit> CopyHistoryItemCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearHistoryCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyAiResponseCommand { get; }

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


    // Nowe właściwości dla plików OBF
    private ObfFile? _obfData;

    public ObfFile? ObfData
    {
        get => _obfData;
        set => this.RaiseAndSetIfChanged(ref _obfData, value);
    }

    // Komendy do wczytywania plików
    public ReactiveCommand<string, Unit> LoadObfFileCommand { get; }

    // Ścieżka do pliku OBF (możesz dostosować)
    private readonly string _obfFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC",
        "data.obf");
    private bool _isInitialized;
    
    public MainViewModel()
    {
        Buttons = [];
        SelectedButtons = [];

        AiResponseHistory = [];
        LoadHistory();

        ReactiveCommand.Create(ExitApplication);

        // Inicjalizacja TTS w zależności od platformy
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _ttsService = new MacTtsService();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ttsService = new WindowsTtsService();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _ttsService = new LinuxTtsService();
        }
        else
        {
            throw new PlatformNotSupportedException("Platforma nie jest wspierana przez TTS.");
        }

        LoadObfFileCommand = ReactiveCommand.CreateFromTask<string>(LoadObfFileAsync);
        ClearSelectedCommand = ReactiveCommand.Create(ClearSelected);
        ButtonClickedCommand = ReactiveCommand.CreateFromTask<Button>(OnButtonClickedAsync);
        RemoveButtonCommand = ReactiveCommand.Create<Button>(OnRemoveButton);
        SpeakCommand = ReactiveCommand.Create(OnSpeak);

        OpenSettingsCommand = ReactiveCommand.Create(OnOpenSettings);


        // Tworzenie poleceń ReactiveCommand z ustawionym schedulerem
        SendToAiCommand = ReactiveCommand.CreateFromTask(OnSendToAiAsync, outputScheduler: RxApp.MainThreadScheduler);

        SpeakAiResponseCommand =
            ReactiveCommand.CreateFromTask(OnSpeakAiResponseAsync, outputScheduler: RxApp.MainThreadScheduler);

        ReactiveCommand.Create(ToggleFullScreen);
        CopySentenceCommand = ReactiveCommand.Create(OnCopySentence);
        CopyHistoryItemCommand = ReactiveCommand.Create<string>(CopyToClipboard);

        ClearHistoryCommand = ReactiveCommand.Create(() =>
        {
            AiResponseHistory.Clear();
            SaveHistory();
        });
        CopyAiResponseCommand = ReactiveCommand.Create(OnCopyAiResponse);


        // Subskrypcje na zmiany w SelectedButtons
        SelectedButtons.CollectionChanged += (_, _) => UpdateConstructedSentence();


        // Wczytanie początkowych danych
        LoadInitialFile();

        LoadMainBoardCommand = ReactiveCommand.CreateFromTask(LoadInitialFile);
    
    }

    public ReactiveCommand<Unit, Unit> LoadMainBoardCommand { get; set; }

    public ReactiveCommand<Unit, Unit> ClearSelectedCommand { get; set; }

    private void ClearSelected()
    {
        SelectedButtons.Clear();
    }

    private async Task LoadInitialFile()
    {
        if (_isInitialized || string.IsNullOrEmpty(ConfigViewModel.Instance.DefaultBoardPath))
        {
            return;
        }

        IsLoading = true;
        try
        {
            await LoadObfOrObzFileAsync(ConfigViewModel.Instance.DefaultBoardPath);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading initial file: {ex.Message}");
            // Możesz tutaj dodać obsługę błędów, np. wyświetlenie komunikatu użytkownikowi
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadObfOrObzFileAsync(string filePath)
    {
        if (filePath.EndsWith(".obz"))
        {
            await LoadObzFileAsync(filePath);
        }
        else if (filePath.EndsWith(".obf"))
        {
            await LoadObfFileAsync(filePath);
        }
        else
        {
            Console.WriteLine("Unsupported file type. Please provide a .obf or .obz file.");
        }
    }

    private async Task LoadObfFileAsync(string filePath)
    {
        try
        {
            IsLoading = true;
            Console.WriteLine($"Rozpoczynanie wczytywania pliku OBF: {filePath}");
            ObfData = await ObfLoader.LoadObfAsync(filePath);

            if (ObfData != null)
            {
                Console.WriteLine("Plik OBF został pomyślnie wczytany.");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Buttons.Clear(); // Clear existing buttons
                    foreach (var button in ObfData.Buttons)
                    {
                        Buttons.Add(button);
                    }
                });
            }
            else
            {
                Console.WriteLine("Plik OBF jest pusty lub niepoprawny.");
            }
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
            ZipFile.ExtractToDirectory(filePath, tempDirectory);

            var manifestPath = Path.Combine(tempDirectory, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson);

                // Assume root is directly provided or identifiable
                var rootObfPath = Path.Combine(tempDirectory, manifest?.Root ?? "root.obf");
                if (File.Exists(rootObfPath))
                {
                    ObfData = await ObfLoader.LoadObfAsync(rootObfPath);
                    Console.WriteLine(ObfData != null
                        ? $"Root OBF file loaded successfully: {rootObfPath}"
                        // Process buttons and other data as needed
                        : "Root OBF file is empty or invalid.");
                    if (ObfData is not null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Buttons.Clear(); // Clear existing buttons
                            foreach (var button in ObfData.Buttons)
                            {
                                Buttons.Add(button);
                            }
                        });
                    }
                }

                // Load only images for other board files
                if (manifest?.Paths.Boards.Values != null)
                {
                    var tasks = manifest.Paths.Boards.Values
                        .Where(boardPath => Path.Combine(tempDirectory, boardPath) != rootObfPath)
                        .Select(boardPath => ObfLoader.LoadObfAsync(Path.Combine(tempDirectory, boardPath)));

                    await Task.WhenAll(tasks);
                }
            }
            else
            {
                Console.WriteLine("Manifest file not found in the .obz package.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading OBZ file: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            // Optionally, delete temp directory to clean up
            Directory.Delete(tempDirectory, true);
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


    private async Task OnGenerateAiTextAsync()
    {
        // Implementacja generowania tekstu z AI na podstawie wybranych ikon
        // Możesz wykorzystać istniejącą logikę OnSendToAiAsync


        // Uruchomienie AI
        await OnSendToAiAsync();

        // Jeśli odpowiedź AI została wygenerowana, odczytaj ją
        if (!string.IsNullOrWhiteSpace(AiResponse))
        {
            await OnSpeakAiResponseAsync();
        }
    }


    // Metoda do dodawania odpowiedzi AI do historii
    private void AddAiResponseToHistory(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return;
        AiResponseHistory.Add(new AiResponse(response));
        SaveHistory();
    }

    // Ścieżka do pliku z historią
    private readonly string _historyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC",
        "ai_response_history.json");

    // Metoda do zapisywania historii do pliku
    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!Directory.Exists(directory))
            {
                if (directory != null) Directory.CreateDirectory(directory);
            }

            // Serializacja z użyciem JsonSerializerOptions, aby obsłużyć daty
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(AiResponseHistory, options);
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            // Logowanie błędu lub obsługa wyjątków
            Console.WriteLine($"Błąd podczas zapisywania historii: {ex.Message}");
        }
    }

    // Metoda do ładowania historii z pliku
    private void LoadHistory()
    {
        try
        {
            if (!File.Exists(_historyFilePath)) return;
            var json = File.ReadAllText(_historyFilePath);
            var history = JsonSerializer.Deserialize<ObservableCollection<AiResponse>>(json);
            if (history == null) return;
            foreach (var response in history)
            {
                AiResponseHistory.Add(response);
            }
        }
        catch (Exception ex)
        {
            // Logowanie błędu lub obsługa wyjątków
            Console.WriteLine($"Błąd podczas ładowania historii: {ex.Message}");
        }
    }

    private Task OnButtonClickedAsync(Button button)
    {
        Console.WriteLine(
            $"OnButtonClicked wykonywane na wątku {System.Threading.Thread.CurrentThread.ManagedThreadId}");

        if (button.LoadBoard is not null)
        {
            _ = LoadObfOrObzFileAsync(button.LoadBoard.Url);
        }
        else
        {
            if (SelectedButtons.Contains(button)) return Task.CompletedTask;
            SelectedButtons.Add(button);
            Console.WriteLine($"Dodano piktogram: {button.Id}");

            // Aktualizacja zdania
            UpdateConstructedSentence();
        }

        return Task.CompletedTask;
    }

    private void OnRemoveButton(Button button)
    {
        Console.WriteLine(
            $"OnRemoveButton wykonywane na wątku {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        if (!SelectedButtons.Contains(button)) return;
        SelectedButtons.Remove(button);
        Console.WriteLine($"Usunięto piktogram: {button.Id}");
    }

    private void UpdateConstructedSentence()
    {
        // Poprawka: Pobieranie Text z obiektu Keyword
        ConstructedSentence = string.Join(" ",
            SelectedButtons.Select(p => p.Label));
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
                Console.WriteLine($"Mówienie: {sentence}");
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
            Console.WriteLine($"Wysyłanie zapytania do Ollama: {ConstructedSentence}");

            // Tworzenie zapytania do Ollama
            var chatRequest = new ChatRequest
            {
                Model = ConfigViewModel.Instance.SelectedModel,
                Prompt = ConstructedSentence,
                Form = SelectedForm,
                Tense = SelectedTense,
                Quantity = Quantity
            };

            var response = await _ollamaClient.ChatAsync(chatRequest).ConfigureAwait(false);

            // Łączenie odpowiedzi z IAsyncEnumerable<string> w jeden string
            var fullResponse = await CombineAsyncEnumerableAsync(response).ConfigureAwait(false);

            AiResponse = fullResponse;
            Console.WriteLine($"Odpowiedź AI: {AiResponse}");
        }
        catch (Exception ex)
        {
            AiResponse = $"Błąd: {ex.Message}";
            Console.WriteLine($"Błąd podczas komunikacji z Ollama: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            AddAiResponseToHistory(AiResponse);
            OnCopyAiResponse();
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
                Console.WriteLine($"Mówienie odpowiedzi AI: {AiResponse}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odczytywania odpowiedzi AI: {ex.Message}");
            }
        }
    }

    private void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
        Console.WriteLine($"ToggleFullScreen wykonane. IsFullScreen: {IsFullScreen}");
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
        // Kopiowanie tekstu do schowka
        Dispatcher.UIThread.Post(() =>
        {
            switch (Application.Current?.ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }:
                    window.Clipboard?.SetTextAsync(textToClipboard);
                    break;
                case ISingleViewApplicationLifetime { MainView: { } mainView }:
                {
                    var visualRoot = mainView.GetVisualRoot();
                    if (visualRoot is TopLevel topLevel)
                    {
                        topLevel.Clipboard?.SetTextAsync(textToClipboard);
                    }

                    break;
                }
                default:
                    Console.WriteLine("Clipboard nie jest dostępny.");
                    break;
            }
        });
    }
}