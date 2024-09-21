using Avalonia.Threading;
using ChatAAC.Models;
using ChatAAC.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ChatAAC.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<Pictogram> Pictograms { get; set; } = new ObservableCollection<Pictogram>();
    public ObservableCollection<Pictogram> SelectedPictograms { get; set; } = new ObservableCollection<Pictogram>();

    private readonly PictogramService _pictogramService;
    private readonly OllamaClient _ollamaClient; // Klient OllamaSharp
    private readonly ITtsService _ttsService; // Interfejs TTS

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchQuery, value);
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
    public ReactiveCommand<Unit, Unit> ToggleFullScreenCommand { get; } // New command

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

    private bool _isFullScreen = false; // New property
    public bool IsFullScreen
    {
        get => _isFullScreen;
        set => this.RaiseAndSetIfChanged(ref _isFullScreen, value);
    }
    private List<Pictogram>? _allPictograms = [];

    
    public MainViewModel()
    {
        _pictogramService = new PictogramService();
        _ollamaClient = new OllamaClient("http://localhost:11434"); // Upewnij się, że adres jest poprawny

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

        PictogramClickedCommand = ReactiveCommand.Create<Pictogram>(OnPictogramClicked);
        RemovePictogramCommand = ReactiveCommand.Create<Pictogram>(OnRemovePictogram);
        SpeakCommand = ReactiveCommand.Create(OnSpeak);

        // Tworzenie poleceń ReactiveCommand z ustawionym schedulerem
        SendToAiCommand = ReactiveCommand.CreateFromTask(OnSendToAiAsync, outputScheduler: RxApp.MainThreadScheduler);
        SpeakAiResponseCommand = ReactiveCommand.CreateFromTask(OnSpeakAiResponseAsync, outputScheduler: RxApp.MainThreadScheduler);
        
        ToggleFullScreenCommand = ReactiveCommand.Create(ToggleFullScreen);
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
            if (_allPictograms != null)
            {
                Console.WriteLine($"Pobrano {_allPictograms.Count} piktogramów.");

                foreach (var pictogram in _allPictograms.Take(100)) // Wczytaj pierwsze 100 piktogramów
                {
                    // Dodaj piktogram do kolekcji na głównym wątku UI
                    await Dispatcher.UIThread.InvokeAsync(() => Pictograms.Add(pictogram));
                    Console.WriteLine($"Dodano piktogram ID: {pictogram.Id}");
                    await _pictogramService.DownloadPictogramImageAsync(pictogram.Id).ConfigureAwait(false);
                }
            }

            Console.WriteLine("Piktogramy zostały załadowane i wyświetlone.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd w LoadPictogramsAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterPictograms()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            Pictograms.Clear();
            if (_allPictograms == null) return;
            foreach (var pictogram in _allPictograms.Take(100))
            {
                Pictograms.Add(pictogram);
            }
        }
        else
        {
            if (_allPictograms == null) return;
            var filtered = _allPictograms
                .Where(p => p.Keywords.Any(k => k.Text.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)))
                .Take(100);

            Pictograms.Clear();
            foreach (var pictogram in filtered)
            {
                Pictograms.Add(pictogram);
            }
        }
    }

    private void OnPictogramClicked(Pictogram pictogram)
    {
        Console.WriteLine($"OnPictogramClicked executing on thread {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        if (SelectedPictograms.Contains(pictogram)) return;
        SelectedPictograms.Add(pictogram);
        Console.WriteLine($"Dodano piktogram: {pictogram.Id}");
    }

    private void OnRemovePictogram(Pictogram pictogram)
    {
        Console.WriteLine($"OnRemovePictogram executing on thread {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        if (!SelectedPictograms.Contains(pictogram)) return;
        SelectedPictograms.Remove(pictogram);
        Console.WriteLine($"Usunięto piktogram: {pictogram.Id}");
    }

    private void UpdateConstructedSentence()
    {
        ConstructedSentence = string.Join(" ", SelectedPictograms.Select(p => p.Keywords[0].Text));
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
    }
}