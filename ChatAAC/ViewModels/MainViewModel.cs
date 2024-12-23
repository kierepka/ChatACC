using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ChatAAC.Models;
using ChatAAC.Models.Obf;
using ChatAAC.Services;
using ChatAAC.Views;
using ReactiveUI;
using Button = ChatAAC.Models.Obf.Button;

namespace ChatAAC.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Fields

    private readonly OllamaClient _ollamaClient = new();
    private readonly ITtsService _ttsService;

    private string _selectedTense = "Teraźniejszy";
    private string _selectedForm = "Oznajmująca";
    private int _quantity = 1;
    private bool _isConfigBarVisible;
    private string _constructedSentence = string.Empty;
    private string _aiResponse = string.Empty;
    private bool _isLoading;
    private bool _isFullScreen;
    private ObfFile? _obfData;
    private int _currentBoardIndex;
    private int _gridRows;
    private int _gridColumns;
    private bool _isInitialized;
    // Field to store history of loaded files
    private List<string> _obfFileHistory = [];
    private int _currentHistoryIndex = -1; // Indeks bieżącego pliku w historii

    private readonly string _historyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC",
        "ai_response_history.json");

    #endregion

    #region Properties

    public ObservableCollection<ButtonViewModel> Buttons { get; } = [];
    public ObservableCollection<Button> SelectedButtons { get; } = [];
    public ObservableCollection<AiResponse> AiResponseHistory { get; } = [];

    public string SelectedTense
    {
        get => _selectedTense;
        set => this.RaiseAndSetIfChanged(ref _selectedTense, value);
    }

    public string SelectedForm
    {
        get => _selectedForm;
        set => this.RaiseAndSetIfChanged(ref _selectedForm, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => this.RaiseAndSetIfChanged(ref _quantity, value);
    }

    public bool IsConfigBarVisible
    {
        get => _isConfigBarVisible;
        set => this.RaiseAndSetIfChanged(ref _isConfigBarVisible, value);
    }

    public string ConstructedSentence
    {
        get => _constructedSentence;
        private set => this.RaiseAndSetIfChanged(ref _constructedSentence, value);
    }

    public string AiResponse
    {
        get => _aiResponse;
        set => this.RaiseAndSetIfChanged(ref _aiResponse, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool IsFullScreen
    {
        get => _isFullScreen;
        set => this.RaiseAndSetIfChanged(ref _isFullScreen, value);
    }

    public ObfFile? ObfData
    {
        get => _obfData;
        set => this.RaiseAndSetIfChanged(ref _obfData, value);
    }

    public int CurrentBoardIndex
    {
        get => _currentBoardIndex;
        set => this.RaiseAndSetIfChanged(ref _currentBoardIndex, value);
    }

    public int GridRows
    {
        get => _gridRows;
        set => this.RaiseAndSetIfChanged(ref _gridRows, value);
    }

    public int GridColumns
    {
        get => _gridColumns;
        set => this.RaiseAndSetIfChanged(ref _gridColumns, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<Unit, Unit> OpenHistoryCommand { get; }
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
    public ReactiveCommand<Unit, Unit> NextBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousBoardCommand { get; }
    public ReactiveCommand<string, Unit> SelectTenseCommand { get; }
    public ReactiveCommand<string, Unit> SelectFormCommand { get; }

    #endregion

    #region Constructor

    public MainViewModel()
    {
        // Wczytujemy ustawienia
        ConfigViewModel.Instance.ReloadSettings();


        // Initialize TTS service based on the platform
        _ttsService = InitializeTtsService();

        // Initialize commands
        OpenSettingsCommand = ReactiveCommand.Create(() => OpenSettings());
        LoadMainBoardCommand = ReactiveCommand.CreateFromTask(LoadMainBoard);
        ClearSelectedCommand = ReactiveCommand.Create(ClearSelected);
        ButtonClickedCommand = ReactiveCommand.CreateFromTask<Button>(OnButtonClickedAsync);
        RemoveButtonCommand = ReactiveCommand.Create<Button>(RemoveSelectedButton);
        SpeakCommand = ReactiveCommand.Create(SpeakConstructedSentence);
        SendToAiCommand = ReactiveCommand.CreateFromTask(SendToAiAsync);
        SpeakAiResponseCommand = ReactiveCommand.CreateFromTask(SpeakAiResponseAsync);
        CopySentenceCommand = ReactiveCommand.Create(CopyConstructedSentenceToClipboard);
        CopyHistoryItemCommand = ReactiveCommand.Create<string>(CopyToClipboard);
        ClearHistoryCommand = ReactiveCommand.Create(ClearHistory);
        OpenHistoryCommand = ReactiveCommand.Create(OpenHistoryFolder);
        CopyAiResponseCommand = ReactiveCommand.Create(CopyAiResponseToClipboard);
        NextBoardCommand = ReactiveCommand.CreateFromTask(LoadNextBoardAsync);
        PreviousBoardCommand = ReactiveCommand.CreateFromTask(LoadPreviousBoardAsync);
        SelectTenseCommand = ReactiveCommand.Create<string>(tense => SelectedTense = tense);
        SelectFormCommand = ReactiveCommand.Create<string>(form => SelectedForm = form);
        // Subscribe to changes in SelectedButtons
        SelectedButtons.CollectionChanged += (_, _) => UpdateConstructedSentence();

        // Load AI response history
        LoadHistory();

        // Load initial OBF or OBZ file
        _ = LoadInitialFileAsync();
    }

    private async Task LoadMainBoard()
    {
        if (_obfFileHistory.Count == 0)
        {
            Console.WriteLine("Historia jest pusta.");
            return;
        }
        
        var nextFilePath = _obfFileHistory[0];
        await LoadObfFileAsync(nextFilePath);
    }

    private void OpenHistoryFolder()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) Directory.CreateDirectory(directory!);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening history folder: {ex.Message}");
        }
    }

    #endregion

    #region Initialization Methods

    private ITtsService InitializeTtsService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacTtsService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsTtsService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxTtsService();

        throw new PlatformNotSupportedException("The platform is not supported for TTS.");
    }

    private async Task LoadInitialFileAsync()
    {
        if (_isInitialized)
            return;

        IsLoading = true;
        try
        {
            var defaultBoardPath = ConfigViewModel.Instance.DefaultBoardPath;

            if (!string.IsNullOrEmpty(defaultBoardPath))
            {
                if (!ConfigViewModel.Instance.BoardPaths.Contains(defaultBoardPath))
                    ConfigViewModel.Instance.BoardPaths.Add(defaultBoardPath);

                CurrentBoardIndex = ConfigViewModel.Instance.BoardPaths.IndexOf(defaultBoardPath);
                ObzDirectoryName = string.Empty;
                await LoadObfOrObzFileAsync(defaultBoardPath);
            }
            else
            {
                // Brak domyślnej tablicy, otwieramy okno ustawień z informacją
                await Dispatcher.UIThread.InvokeAsync(() => { OpenSettings("Proszę wybrać domyślną tablicę."); });
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

    #endregion

    #region Board Navigation Methods

    private async Task LoadNextBoardAsync()
    {
        if (_obfFileHistory.Count == 0)
        {
            Console.WriteLine("Historia jest pusta.");
            return;
        }

        // Sprawdź, czy można przejść do przodu
        if (_currentHistoryIndex + 1 < _obfFileHistory.Count)
        {
            _currentHistoryIndex++;
            var nextFilePath = _obfFileHistory[_currentHistoryIndex];
            await LoadObfFileAsync(nextFilePath);
        }
        else
        {
            Console.WriteLine("Brak kolejnych tablic w historii.");
        }
    }

    private async Task LoadPreviousBoardAsync()
    {
        if (_obfFileHistory.Count == 0)
        {
            Console.WriteLine("Historia jest pusta.");
            return;
        }

        // Sprawdź, czy można przejść wstecz
        if (_currentHistoryIndex > 0)
        {
            _currentHistoryIndex--;
            var previousFilePath = _obfFileHistory[_currentHistoryIndex];
            await LoadObfFileAsync(previousFilePath);
        }
        else
        {
            Console.WriteLine("Brak poprzednich tablic w historii.");
        }
    }

    #endregion

    #region File Loading Methods

    private async Task LoadObfOrObzFileAsync(string filePath)
    {
        if (filePath.EndsWith(".obz", StringComparison.OrdinalIgnoreCase))
            await LoadObzFileAsync(filePath);
        else if (filePath.EndsWith(".obf", StringComparison.OrdinalIgnoreCase))
        {
            var obfFileName = Path.GetFileName(filePath);
            var cachedObfPath = Path.Combine(ObfCacheDirectory, ObzDirectoryName, obfFileName);


            await LoadObfFileAsync(cachedObfPath);
        }
        else
            Console.WriteLine("Nieobsługiwany typ pliku. Podaj plik z rozszerzeniem .obf lub .obz.");
    }

    
    // Metoda pomocnicza do ładowania pliku OBF i aktualizacji historii
    private async Task LoadObfFileAsync(string filePath)
    {
        IsLoading = true;
        try
        {
            var obfFile = await ObfLoader.LoadObfAsync(filePath);
            if (obfFile == null)
            {
                Console.WriteLine("Plik OBF jest pusty lub nieprawidłowy.");
                return;
            }

            ObfData = obfFile;
            CurrentObfFilePath = filePath;

            // Jeśli wczytujemy nowy plik spoza historii
            if (_currentHistoryIndex == -1 || _currentHistoryIndex == _obfFileHistory.Count - 1 || _obfFileHistory[_currentHistoryIndex] != filePath)
            {
                // Usuń elementy z historii po bieżącym indeksie (jeśli poruszamy się w środku historii)
                if (_currentHistoryIndex < _obfFileHistory.Count - 1)
                {
                    _obfFileHistory = _obfFileHistory.Take(_currentHistoryIndex + 1).ToList();
                }

                // Dodaj nowy plik do historii
                _obfFileHistory.Add(filePath);
                _currentHistoryIndex = _obfFileHistory.Count - 1;
            }

            // Załaduj przyciski z pliku OBF
            LoadButtonsFromObfData(ObfData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas ładowania pliku OBF: {filePath}. Szczegóły: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public string CurrentObfFilePath { get; set; }

    /// <summary>
    /// Asynchronously loads an OBZ file from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the OBZ file.</param>
    /// <returns>An asynchronous task.</returns>
    private async Task LoadObzFileAsync(string filePath)
    {
        // Set the loading state to true
        IsLoading = true;

        try
        {
            // Create a subdirectory with the name corresponding to the .obz file
            ObzDirectoryName = Path.GetFileNameWithoutExtension(filePath);
            var destinationDirectory = Path.Combine(ObfCacheDirectory, ObzDirectoryName);

            // Extract the contents of the OBZ file
            await ExtractObzArchiveAsync(filePath);

            var manifestPath = Path.Combine(destinationDirectory, "manifest.json");
            if (File.Exists(manifestPath))
            {
                // Read the manifest file
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson);

                // Construct the path to the root OBF file
                var rootObfPath = Path.Combine(destinationDirectory, manifest?.Root ?? "root.obf");
                if (File.Exists(rootObfPath))
                    // Load the root OBF file
                    await LoadObfFileAsync(rootObfPath);
                else
                    Console.WriteLine("root.obf file not found in the OBZ package.");
            }
            else
            {
                Console.WriteLine("manifest.json file not found in the OBZ package.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the loading process
            Console.WriteLine($"Error loading OBZ file: {ex.Message}");
        }
        finally
        {
            // Set the loading state to false
            IsLoading = false;
        }
    }
    

    private string ObzDirectoryName { get; set; } = string.Empty;

    private static string ObfCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Obf");

    private static string PictogramsCacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Pictograms");


    private async Task ExtractObzArchiveAsync(string filePath)
    {
        // Extract the directory name from the .obz file

        var pictogramsPath = Path.Combine(PictogramsCacheDirectory, ObzDirectoryName);
        var obfPath = Path.Combine(ObfCacheDirectory, ObzDirectoryName);

        // Ensure the target directories exist
        Directory.CreateDirectory(pictogramsPath);
        Directory.CreateDirectory(obfPath);

        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(filePath);
            foreach (var entry in archive.Entries)
            {
                // Sanitize the entry name to prevent directory traversal attacks
                var sanitizedEntryName = SanitizeEntryName(entry.FullName);

                // Determine the destination directory based on the file type
                string? destinationDirectory = null;

                if (IsImageFile(sanitizedEntryName))
                {
                    destinationDirectory = pictogramsPath;
                }
                else if (IsObfFile(sanitizedEntryName))
                {
                    destinationDirectory = obfPath;
                }
                else if (IsManifestFile(sanitizedEntryName))
                {
                    destinationDirectory = obfPath;
                }

                if (destinationDirectory == null) continue;
                // Build the full destination path within the target directory
                var destinationPath =
                    Path.GetFullPath(Path.Combine(destinationDirectory, Path.GetFileName(sanitizedEntryName)));

                // Skip unsafe entries
                if (!destinationPath.StartsWith(destinationDirectory, StringComparison.Ordinal))
                {
                    Console.WriteLine($"Skipped unsafe entry: {entry.FullName}");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    // Create directory if the entry is a folder
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    // Ensure the destination directory exists and extract the file
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        });
    }

    private bool IsManifestFile(string fileName)
    {
        return fileName.ToLower().Equals("manifest.json");
    }

    /// <summary>
    /// Checks if the file is an image file based on its extension.
    /// </summary>
    private static bool IsImageFile(string fileName)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        return extensions.Contains(fileExtension);
    }

    /// <summary>
    /// Checks if the file is an OBF file based on its extension.
    /// </summary>
    private static bool IsObfFile(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() == ".obf";
    }

    private string SanitizeEntryName(string entryName)
    {
        // Replace backslashes with slashes
        entryName = entryName.Replace('\\', '/');

        // Remove drive letters or root indicators
        entryName = MyRegex().Replace(entryName, "");

        // Remove any leading slashes
        entryName = entryName.TrimStart('/');

        // Remove "../" and "./" components
        var segments = entryName.Split('/');
        var sanitizedSegments = segments.Where(segment => segment != ".." && segment != ".").ToArray();

        return Path.Combine(sanitizedSegments);
    }

    #endregion

    #region Button Handling Methods

    private void LoadButtonsFromObfData(ObfFile obfFile)
    {
        // Czyszczenie istniejących przycisków
        Buttons.Clear();

        // Mapowanie przycisków z OBF na słownik dla szybkiego dostępu po ID
        Dictionary<int, Button> buttonDictionary = obfFile.Buttons.ToDictionary(b => b.Id, b => b);

        if (obfFile.Grid != null)
        {
            var rowIndex = 0;
            foreach (var row in obfFile.Grid.Order)
            {
                var columnIndex = 0;
                foreach (var buttonId in row)
                {
                    if (buttonId != null && buttonDictionary.TryGetValue(buttonId.Value, out var button))
                    {
                        var buttonViewModel = new ButtonViewModel(button, rowIndex, columnIndex);
                        Buttons.Add(buttonViewModel);
                    }

                    columnIndex++;
                }

                rowIndex++;
            }

            GridRows = obfFile.Grid.Rows;
            GridColumns = obfFile.Grid.Columns;
        }
        else
        {
            foreach (var buttonViewModel in obfFile.Buttons.Select(button => new ButtonViewModel(button, 0, 0)))
                Buttons.Add(buttonViewModel);

            GridRows = 1;
            GridColumns = Buttons.Count;
        }
    }

    private async Task OnButtonClickedAsync(Button button)
    {
        Console.WriteLine($"Kliknięto przycisk: {button.Label}");

        if (button.LoadBoard != null && !string.IsNullOrEmpty(button.LoadBoard.Path))
        {
            await LoadObfOrObzFileAsync(button.LoadBoard.Path);
        }
        else
        {
            if (SelectedButtons.Contains(button)) return;
            SelectedButtons.Add(button);
        }
    }

    private void RemoveSelectedButton(Button button)
    {
        if (SelectedButtons.Contains(button)) SelectedButtons.Remove(button);
    }

    private void ClearSelected()
    {
        SelectedButtons.Clear();
    }

    #endregion

    #region Sentence Construction Methods

    private void UpdateConstructedSentence()
    {
        ConstructedSentence = string.Join(" ", SelectedButtons.Select(p => p.Label));
        Console.WriteLine($"Constructed sentence: {ConstructedSentence}");
    }

    private void SpeakConstructedSentence()
    {
        var sentence = ConstructedSentence;
        Task.Run(async () =>
        {
            try
            {
                await _ttsService.SpeakAsync(sentence).ConfigureAwait(false);
                Console.WriteLine($"Spoken sentence: {sentence}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error speaking text: {ex.Message}");
            }
        });
    }

    #endregion

    #region AI Interaction Methods

    private async Task SendToAiAsync()
    {
        if (string.IsNullOrWhiteSpace(ConstructedSentence))
        {
            AiResponse = "No buttons selected.";
            return;
        }

        try
        {
            IsLoading = true;
            AiResponse = "Generating response...";
            Console.WriteLine($"Sending query: {ConstructedSentence}");

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

            Console.WriteLine($"AI Response: {AiResponse}");
        }
        catch (Exception ex)
        {
            AiResponse = $"Error: {ex.Message}";
            Console.WriteLine($"Error communicating with AI: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            AddAiResponseToHistory(AiResponse);
            CopyAiResponseToClipboard();
            await SpeakAiResponseAsync();
        }
    }

    private async Task<string> CombineAsyncEnumerableAsync(IAsyncEnumerable<string> asyncStrings)
    {
        var stringBuilder = new StringBuilder();
        await foreach (var str in asyncStrings.ConfigureAwait(false)) stringBuilder.Append(str);

        return stringBuilder.ToString();
    }

    private async Task SpeakAiResponseAsync()
    {
        if (!string.IsNullOrEmpty(AiResponse))
            try
            {
                await _ttsService.SpeakAsync(AiResponse).ConfigureAwait(false);
                Console.WriteLine($"Spoken AI response: {AiResponse}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error speaking AI response: {ex.Message}");
            }
    }

    #endregion

    #region Clipboard Methods

    private void CopyAiResponseToClipboard()
    {
        if (!string.IsNullOrEmpty(AiResponse)) CopyToClipboard(AiResponse);
    }

    private void CopyConstructedSentenceToClipboard()
    {
        if (!string.IsNullOrEmpty(ConstructedSentence)) CopyToClipboard(ConstructedSentence);
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
                    Console.WriteLine("Clipboard is not available.");
                    break;
            }
        });
    }

    #endregion

    #region History Methods

    private void AddAiResponseToHistory(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return;
        AiResponseHistory.Add(new AiResponse(response));
        SaveHistory();
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(AiResponseHistory, options);
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving history: {ex.Message}");
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
            Console.WriteLine($"Error loading history: {ex.Message}");
        }
    }

    private void ClearHistory()
    {
        AiResponseHistory.Clear();
        SaveHistory();
    }

    #endregion

    #region Settings

    private async void OpenSettings(string? message = null)
    {
        try
        {
            if (message != null) ConfigViewModel.Instance.Message = message;

            var configWindow = new ConfigWindow
            {
                DataContext = ConfigViewModel.Instance
            };

            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow;

            if (mainWindow == null) return;
            await configWindow.ShowDialog(mainWindow);

            configWindow.Closed += async (_, _) =>
            {
                await OnConfigWindowClosedAsync();
                ConfigViewModel.Instance.Message = null;
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening settings window: {ex.Message}");
        }
    }

    private async Task OnConfigWindowClosedAsync()
    {
        try
        {
            // Wymuszamy zapis konfiguracji przed zamknięciem
            ConfigViewModel.Instance.SaveCommand.Execute().Subscribe();

            // Zapamiętujemy aktualną ścieżkę
            var currentBoardPath = ConfigViewModel.Instance.BoardPaths.ElementAtOrDefault(CurrentBoardIndex);

            // Przeładowanie ustawień
            ConfigViewModel.Instance.ReloadSettings();

            // Sprawdzamy czy konfiguracja została poprawnie zapisana
            var savedBoardPaths = ConfigViewModel.Instance.BoardPaths;
            if (!savedBoardPaths.Any()) Console.WriteLine("Warning: Board paths are empty after reload");

            // Czyścimy stan aplikacji
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Buttons.Clear();
                SelectedButtons.Clear();
                AiResponse = string.Empty;
                ConstructedSentence = string.Empty;
            });

            _isInitialized = false;

            // Przywracamy pozycję w BoardPaths
            if (!string.IsNullOrEmpty(currentBoardPath))
            {
                CurrentBoardIndex = ConfigViewModel.Instance.BoardPaths.IndexOf(currentBoardPath);
                if (CurrentBoardIndex == -1) CurrentBoardIndex = 0;
            }
            else
            {
                CurrentBoardIndex = 0;
            }

            await LoadInitialFileAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnConfigWindowClosedAsync: {ex.Message}");
        }
    }

    [GeneratedRegex(@"^[a-zA-Z]:/")]
    private static partial Regex MyRegex();

    #endregion
}