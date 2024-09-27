using Avalonia.Threading;
using ChatAAC.Models;
using ChatAAC.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using ChatAAC.Views;

namespace ChatAAC.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<Pictogram> Pictograms { get; set; } = [];
    public ObservableCollection<Pictogram> SelectedPictograms { get; set; } = [];

    public ObservableCollection<Category> Categories { get; set; } = [];
    public ObservableCollection<Tag> Tags { get; set; } = [];

    private readonly PictogramService _pictogramService;
    private readonly OllamaClient _ollamaClient = new(ConfigViewModel.Instance.OllamaAddress); // Klient OllamaSharp
    private readonly ITtsService _ttsService; // Interfejs TTS
    private LoadingWindow? _loadingWindow;

    private string _searchQuery = string.Empty;
    
    public ObservableCollection<AiResponse> AiResponseHistory { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchQuery, value);
            FilterPictograms();
        }
    }


    private void ExitApplication()
    {
        // Logic to exit the application
        Environment.Exit(0);
    }

    private string _tagSearch = string.Empty;

    public string TagSearch
    {
        get => _tagSearch;
        set
        {
            this.RaiseAndSetIfChanged(ref _tagSearch, value);
            FilterPictograms();
        }
    }

    private Category? _selectedCategory;

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            FilterPictograms();
        }
    }

    private string _constructedSentence = string.Empty;

    public string ConstructedSentence
    {
        get => _constructedSentence;
        private set => this.RaiseAndSetIfChanged(ref _constructedSentence, value);
    }

    public ReactiveCommand<Pictogram, Unit> PictogramClickedCommand { get; }
    public ReactiveCommand<Pictogram, Unit> RemovePictogramCommand { get; }
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

    private List<Pictogram>? _allPictograms = [];
    public IEnumerable<IGrouping<string, Pictogram>> GroupedPictograms =>
        Pictograms.GroupBy(p => p.Categories.FirstOrDefault() ?? "Inne");
    public MainViewModel()
    {
        AiResponseHistory = [];
        LoadHistory();

        _pictogramService = new PictogramService();
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


        PictogramClickedCommand = ReactiveCommand.CreateFromTask<Pictogram>(OnPictogramClickedAsync);
        RemovePictogramCommand = ReactiveCommand.Create<Pictogram>(OnRemovePictogram);
        SpeakCommand = ReactiveCommand.Create(OnSpeak);

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


        // Subskrypcje na zmiany w SelectedPictograms
        SelectedPictograms.CollectionChanged += (_, _) => UpdateConstructedSentence();

        LoadPictogramsAsync();
    }

    private async void LoadPictogramsAsync()
    {
        try
        {
            IsLoading = true;
            Console.WriteLine("Rozpoczynanie pobierania piktogramów...");
            _allPictograms = await _pictogramService.GetAllPictogramsAsync().ConfigureAwait(false);
            if (_allPictograms != null && _allPictograms.Count > 0)
            {
                Console.WriteLine($"Pobrano {_allPictograms.Count} piktogramów.");

                // Wyodrębnij kategorie i tagi
                var categories = _pictogramService.ExtractCategories(_allPictograms);
                var tags = _pictogramService.ExtractTags(_allPictograms);

                // Dodaj kategorie i tagi do ObservableCollection na głównym wątku UI
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Categories.Clear();
                    // Dodanie pustej kategorii na początku

                    Categories.Add(new Category()
                    {
                        Id = "core", Name = "Główne"
                    });

                    Categories.Add(new Category()
                    {
                        Id = string.Empty, Name = "Wszystkie"
                    });


                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }

                    Tags.Clear();
                    foreach (var tag in tags)
                    {
                        Tags.Add(tag);
                    }
                });

                // Ustaw domyślną kategorię, jeśli istnieje
                if (Categories.Any())
                {
                    SelectedCategory = Categories.First();
                }

                // Filtruj piktogramy na podstawie wybranej kategorii i tagów
                FilterPictograms();
            }
            else
            {
                Console.WriteLine("Piktogramy nie zostały pobrane poprawnie.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd w LoadPictogramsAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            // Zamknięcie LoadingWindow
            if (_loadingWindow != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _loadingWindow.Close();
                    _loadingWindow = null;
                });
            }
        }
    }

    private void FilterPictograms()
    {
        Pictograms.Clear();

        if (SelectedCategory == null)
            return;

        var filtered = _allPictograms?.AsEnumerable() ?? [];

        // Filtrowanie na podstawie konfiguracji
        var config = ConfigViewModel.Instance;

        if (!config.ShowSex)
            filtered = filtered.Where(p => !p.Sex);

        if (!config.ShowViolence)
            filtered = filtered.Where(p => !p.Violence);

        /*   if (!config.ShowAac)
               filtered = filtered.Where(p => !p.Aac);

           if (!config.ShowSchematic)
               filtered = filtered.Where(p => !p.Schematic);
   */
        // Dodatkowe filtrowanie dla AacColor, Skin i Hair
        // Zakładam, że te pola powinny być zawsze pokazywane, chyba że zostaną dodane do konfiguracji
        // filtered = filtered.Where(p => p.AacColor);
        // filtered = filtered.Where(p => p.Skin);
        // filtered = filtered.Where(p => p.Hair);

        // Filtrowanie po wybranej kategorii
        if (SelectedCategory != null && !string.IsNullOrWhiteSpace(SelectedCategory.Id))
        {
            switch (SelectedCategory.Id)
            {
                case "core":
                    filtered = filtered.Where(p =>
                        p.Categories.Any(c => c.IndexOf("core", StringComparison.OrdinalIgnoreCase) >= 0));
                    break;
                case "":
                    // Pokazuj wszystkie piktogramy
                    break;
                default:
                    filtered = filtered.Where(p =>
                        p.Categories.Any(c => c.Equals(SelectedCategory.Name, StringComparison.OrdinalIgnoreCase)));
                    break;
            }
        }

        // Filtrowanie po tagach
        if (!string.IsNullOrWhiteSpace(TagSearch))
        {
            var tags = TagSearch.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            filtered = tags.Aggregate(filtered,
                (current, tag) =>
                    current.Where(p => p.Tags.Any(pt => pt.Contains(tag, StringComparison.OrdinalIgnoreCase))));
        }

        // Filtrowanie po zapytaniu wyszukiwania
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            filtered = filtered.Where(p =>
                p.Keywords.Any(k => k.KeywordKeyword.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));
        }

        // Usunięcie piktogramów z pustym polem Text
        filtered = filtered.Where(p => !string.IsNullOrWhiteSpace(p.Text));

        // Sortowanie po KeywordKeyword
        filtered = filtered
            .OrderBy(p => p.Categories.FirstOrDefault())
            .ThenBy(p => p.Tags.FirstOrDefault())
            .ThenBy(p => p.Keywords.FirstOrDefault()?.KeywordKeyword);

        foreach (var pictogram in filtered)
        {
            Pictograms.Add(pictogram);
        }

        Console.WriteLine($"Filtracja zakończona. Liczba piktogramów: {Pictograms.Count}");
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

    private async Task OnPictogramClickedAsync(Pictogram pictogram)
    {
        Console.WriteLine(
            $"OnPictogramClicked wykonywane na wątku {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        if (SelectedPictograms.Contains(pictogram)) return;
        SelectedPictograms.Add(pictogram);
        Console.WriteLine($"Dodano piktogram: {pictogram.Id}");

        // Aktualizacja zdania
        UpdateConstructedSentence();

        // Uruchomienie AI
        await OnSendToAiAsync();

        // Jeśli odpowiedź AI została wygenerowana, odczytaj ją
        if (!string.IsNullOrWhiteSpace(AiResponse))
        {
            await OnSpeakAiResponseAsync();
        }
    }

    private void OnRemovePictogram(Pictogram pictogram)
    {
        Console.WriteLine(
            $"OnRemovePictogram wykonywane na wątku {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        if (!SelectedPictograms.Contains(pictogram)) return;
        SelectedPictograms.Remove(pictogram);
        Console.WriteLine($"Usunięto piktogram: {pictogram.Id}");
    }

    private void UpdateConstructedSentence()
    {
        // Poprawka: Pobieranie Text z obiektu Keyword
        ConstructedSentence = string.Join(" ",
            SelectedPictograms.Select(p => p.Keywords.FirstOrDefault()?.KeywordKeyword ?? ""));
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
            AiResponse = "Brak zdania do wysłania.";
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
                Model = "llava-phi3",
                Prompt = ConstructedSentence
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