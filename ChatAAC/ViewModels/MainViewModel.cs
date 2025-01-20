using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models;
using ChatAAC.Models.Obf;
using ChatAAC.Services;
using ChatAAC.Views;
using ReactiveUI;
using Button = ChatAAC.Models.Obf.Button;

namespace ChatAAC.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Constructor

    public MainViewModel()
    {
        // Load settings
        ConfigViewModel.Instance.ReloadSettings();

        // Initialize TTS service via factory
        _ttsService = TtsServiceFactory.CreateTtsService();

        // Initialize services
        _aiInteractionService = new AiInteractionService();
        _boardLoaderService = new BoardLoaderService(this);
        _historyService = new HistoryService();

        // Initialize commands
        // Initialize ToggleEditModeCommand
        ToggleEditModeCommand = ReactiveCommand.Create(ToggleEditMode);

        EditGridCommand = ReactiveCommand.CreateFromTask(EditGridAsync);
        SaveBoardCommand = ReactiveCommand.CreateFromTask(SaveBoardAsync);
        
        OpenSettingsCommand = ReactiveCommand.Create(() => OpenSettings());
        SelectBoardAndLoadCommand = ReactiveCommand.CreateFromTask(SelectBoardAndLoadAsync);
        ClearSelectedCommand = ReactiveCommand.Create(ClearSelected);
        ButtonClickedCommand = ReactiveCommand.CreateFromTask<Button>(OnButtonClickedAsync);
        RemoveButtonCommand = ReactiveCommand.Create<Button>(RemoveSelectedButton);
        SpeakCommand = ReactiveCommand.Create(SpeakConstructedSentence);
        SendToAiCommand = ReactiveCommand.CreateFromTask(SendToAiAsync);
        SpeakAiResponseCommand = ReactiveCommand.CreateFromTask(SpeakAiResponseAsync);
        CopySentenceCommand = ReactiveCommand.Create(CopyConstructedSentenceToClipboard);
        CopyHistoryItemCommand = ReactiveCommand.Create<string>(ClipboardService.CopyToClipboard);
        OpenHistoryCommand = ReactiveCommand.Create(OpenHistoryWindow);
        CopyAiResponseCommand = ReactiveCommand.Create(CopyAiResponseToClipboard);
        NextBoardCommand = ReactiveCommand.CreateFromTask(LoadNextBoardAsync);
        PreviousBoardCommand = ReactiveCommand.CreateFromTask(LoadPreviousBoardAsync);
        SelectTenseCommand = ReactiveCommand.Create<string>(tense => SelectedTense = tense);
        SelectFormCommand = ReactiveCommand.Create<string>(form => SelectedForm = form);

        SelectedButtons.CollectionChanged += (_, _) => UpdateConstructedSentence();

        // Load AI response history
        _historyService.LoadHistory();

        // Load initial OBF or OBZ file
        _ = LoadInitialFileAsync();
    }

    #endregion

    #region Fields

    private readonly ITtsService _ttsService;
    private readonly AiInteractionService _aiInteractionService;
    private readonly BoardLoaderService _boardLoaderService;
    private readonly HistoryService _historyService;

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

    private bool _isEditMode;

    // Field to store history of loaded files
    private List<string?> _obfFileHistory = [];
    private int _currentHistoryIndex = -1; // Index current file in history 

    #endregion

    #region Properties

    public bool IsEditMode
    {
        get => _isEditMode;
        set => this.RaiseAndSetIfChanged(ref _isEditMode, value);
    }

    public ObservableCollection<ButtonViewModel> Buttons { get; } = new();
    public ObservableCollection<Button> SelectedButtons { get; } = new();
    public ObservableCollection<AiResponse> AiResponseHistory { get; } = new();

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

    private string ConstructedSentence
    {
        get => _constructedSentence;
        set => this.RaiseAndSetIfChanged(ref _constructedSentence, value);
    }

    private string AiResponse
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

    public List<string?> ObfFileHistory
    {
        get => _obfFileHistory;
        set => this.RaiseAndSetIfChanged(ref _obfFileHistory, value);
    }

    public int CurrentHistoryIndex
    {
        get => _currentHistoryIndex;
        set => this.RaiseAndSetIfChanged(ref _currentHistoryIndex, value);
    }

    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> EditGridCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleEditModeCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenHistoryCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectBoardAndLoadCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSelectedCommand { get; }
    public ReactiveCommand<Button, Unit> ButtonClickedCommand { get; }
    public ReactiveCommand<Button, Unit> RemoveButtonCommand { get; }
    public ReactiveCommand<Unit, Unit> SpeakCommand { get; }
    public ReactiveCommand<Unit, Unit> SendToAiCommand { get; }
    public ReactiveCommand<Unit, Unit> SpeakAiResponseCommand { get; }
    public ReactiveCommand<Unit, Unit> CopySentenceCommand { get; }
    public ReactiveCommand<string, Unit> CopyHistoryItemCommand { get; }
    public ReactiveCommand<Unit, Unit> CopyAiResponseCommand { get; }
    public ReactiveCommand<Unit, Unit> NextBoardCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousBoardCommand { get; }
    public ReactiveCommand<string, Unit> SelectTenseCommand { get; }
    public ReactiveCommand<string, Unit> SelectFormCommand { get; }

    #endregion

    #region File Loading / Initialization

    private async Task SelectBoardAndLoadAsync()
    {
        try
        {
            // 1. Ensure we have a desktop environment
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            // 2. Create the popup window + its VM
            var popupVm = new BoardPathsViewModel(ConfigViewModel.Instance.BoardPaths);

            var popupWindow = new BoardPathsWindow
            {
                DataContext = popupVm
            };

            // 3. Show as dialog and wait for user confirmation
            //    BoardPathsViewModel will store the SelectedBoardPath upon OK
            if (desktop.MainWindow != null) await popupWindow.ShowDialog(desktop.MainWindow);

            // 4. If user confirmed a valid path, load it
            if (!string.IsNullOrEmpty(popupVm.SelectedBoardPath))
                await _boardLoaderService.LoadObfOrObzFileAsync(popupVm.SelectedBoardPath);
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Error selecting and loading board. Details: {ex.Message}");
        }
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
                await _boardLoaderService.LoadObfOrObzFileAsync(defaultBoardPath);
            }
            else
            {
                //No default board - show settings window with message
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OpenSettings(
                        Resources.MainViewModel_PleaseSetDefaultBoard);
                });
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.MainViewModel_LoadInitialFileAsync_Błąd_podczas_wczytywania_pliku_początkowego___0_,
                ex.Message));
        }
        finally
        {
            IsLoading = false;
            _isInitialized = true;
        }
    }
    
    public async Task SaveBoardAsync()
    {
        if (string.IsNullOrEmpty(CurrentObfFilePath) || ObfData == null)
        {
            AppLogger.LogInfo("No board file or ObfData is null - cannot save.");
            return;
        }
        await _boardLoaderService.SaveObfFileAsync(CurrentObfFilePath, ObfData);
        AppLogger.LogInfo("Board saved successfully.");
    }

    public string CurrentObfFilePath { get; set; } = string.Empty;

    public string ObzDirectoryName { get; set; } = string.Empty;

    #endregion

    #region Navigation

    private async Task LoadNextBoardAsync()
    {
        if (_obfFileHistory.Count == 0)
        {
            AppLogger.LogInfo(Resources.MainViewModel_LoadNextBoardAsync_Historia_jest_pusta_);
            return;
        }

        if (_currentHistoryIndex + 1 < _obfFileHistory.Count)
        {
            _currentHistoryIndex++;
            var nextFilePath = _obfFileHistory[_currentHistoryIndex];
            await _boardLoaderService.LoadObfFileAsync(nextFilePath);
        }
        else
        {
            AppLogger.LogInfo(Resources.MainViewModel_LoadNextBoardAsync_Brak_kolejnych_tablic_w_historii_);
        }
    }

    private async Task LoadPreviousBoardAsync()
    {
        if (_obfFileHistory.Count == 0)
        {
            AppLogger.LogInfo(Resources.MainViewModel_LoadNextBoardAsync_Historia_jest_pusta_);
            return;
        }

        if (_currentHistoryIndex > 0)
        {
            _currentHistoryIndex--;
            var previousFilePath = _obfFileHistory[_currentHistoryIndex];
            await _boardLoaderService.LoadObfFileAsync(previousFilePath);
        }
        else
        {
            AppLogger.LogInfo(Resources.MainViewModel_LoadPreviousBoardAsync_Brak_poprzednich_tablic_w_historii_);
        }
    }

    #endregion


    #region Methods - Edit Mode

    private void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
        AppLogger.LogInfo($"Edit mode is now {(IsEditMode ? "ON" : "OFF")}");
    }

    private async Task EditGridAsync()
    {
        // Only valid if ObfData?.Grid != null
        if (ObfData?.Grid == null)
        {
            AppLogger.LogInfo("No grid found in the loaded board.");
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var vm = new EditGridViewModel(ObfData.Grid);
        var win = new EditGridWindow
        {
            DataContext = vm
        };

        if (desktop.MainWindow != null) await win.ShowDialog(desktop.MainWindow);

        if (!vm.IsConfirmed)
            return;

        // If user confirmed, changes are in memory
        // We'll re-load or just re-bind buttons, then save
        await SaveBoardAsync();
    }

    private async Task EditButtonAsync(Button button)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            // If your ObfFile has a list of images, pass them to the VM
            var availableImages = ObfData?.Images ?? new List<Image>();
            var editVm = new EditButtonViewModel(button, availableImages);

            var editWindow = new EditButtonWindow
            {
                DataContext = editVm
            };

            if (desktop.MainWindow != null) await editWindow.ShowDialog(desktop.MainWindow);

            if (!editVm.IsConfirmed)
                return;

            // The button is updated in memory. Save
            await SaveBoardAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Error editing button: {ex.Message}");
        }
    }

    #endregion

    #region Button Handling

    private async Task OnButtonClickedAsync(Button button)
    {
        AppLogger.LogInfo(string.Format(
            Resources.MainViewModel_OnButtonClickedAsync_Kliknięto_przycisk___0_, button.Label));

        
        if (!IsEditMode)
        {
            // Normal mode
            if (button.LoadBoard != null && !string.IsNullOrEmpty(button.LoadBoard.Path))
            {
                // load sub-board
                await _boardLoaderService.LoadObfOrObzFileAsync(button.LoadBoard.Path);
            }
            else
            {
                // add to selected
                if (!SelectedButtons.Contains(button))
                    SelectedButtons.Add(button);
            }
        }
        else
        {
            // Edit mode: open edit window
            await EditButtonAsync(button);
        }
        
    }

    private void RemoveSelectedButton(Button button)
    {
        if (SelectedButtons.Contains(button))
            SelectedButtons.Remove(button);
    }

    private void ClearSelected()
    {
        SelectedButtons.Clear();
    }

    #endregion

    #region Sentence Handling

    private void UpdateConstructedSentence()
    {
        ConstructedSentence = string.Join(" ", SelectedButtons.Select(p => p.Label));
        AppLogger.LogInfo(string.Format(
            Resources.MainViewModel_UpdateConstructedSentence_Constructed_sentence___0_,
            ConstructedSentence));
    }

    private void SpeakConstructedSentence()
    {
        var sentence = ConstructedSentence;
        Task.Run(async () =>
        {
            try
            {
                await _ttsService.SpeakAsync(sentence).ConfigureAwait(false);
                AppLogger.LogInfo(string.Format(Resources.MainViewModel_SpeakConstructedSentence_Spoken_sentence___0_,
                    sentence));
            }
            catch (Exception ex)
            {
                AppLogger.LogError(string.Format(
                    Resources.MainViewModel_SpeakConstructedSentence_Error_speaking_text___0_,
                    ex.Message));
            }
        });
    }

    #endregion

    #region AI Interaction

    private async Task SendToAiAsync()
    {
        // Send to AI
        IsLoading = true;
        AiResponse = Resources.MainWindowGeneratingResponse;
        AppLogger.LogInfo(string.Format(Resources.MainViewModel_SendToAiAsync_Sending_query___0_, ConstructedSentence));

        var response = await _aiInteractionService.SendToAiAsync(
            ConstructedSentence,
            SelectedForm,
            SelectedTense,
            Quantity,
            AppLogger.LogInfo
        );

        AiResponse = response;
        AppLogger.LogInfo(string.Format(Resources.MainViewModel_SendToAiAsync_AI_Response___0_, AiResponse));
        IsLoading = false;

        AddAiResponseToHistory(AiResponse);
        CopyAiResponseToClipboard();
        await SpeakAiResponseAsync();
    }

    private async Task SpeakAiResponseAsync()
    {
        if (!string.IsNullOrEmpty(AiResponse))
            try
            {
                await _ttsService.SpeakAsync(AiResponse).ConfigureAwait(false);
                AppLogger.LogInfo(string.Format(Resources.MainViewModel_SpeakAiResponseAsync_Spoken_AI_response___0_,
                    AiResponse));
            }
            catch (Exception ex)
            {
                AppLogger.LogError(string.Format(
                    Resources.MainViewModel_SpeakAiResponseAsync_Error_speaking_AI_response___0_,
                    ex.Message));
            }
    }

    #endregion

    #region Clipboard Handling

    private void CopyAiResponseToClipboard()
    {
        if (!string.IsNullOrEmpty(AiResponse)) ClipboardService.CopyToClipboard(AiResponse);
    }

    private void CopyConstructedSentenceToClipboard()
    {
        if (!string.IsNullOrEmpty(ConstructedSentence)) ClipboardService.CopyToClipboard(ConstructedSentence);
    }

    #endregion

    #region History

    private void AddAiResponseToHistory(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return;
        _historyService.AddToHistory(new AiResponse(response));
        _historyService.SaveHistory();
    }

    private void OpenHistoryWindow()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
                return;

            var historyWindow = new HistoryWindow
            {
                DataContext = new HistoryViewModel(_historyService.HistoryItems, _historyService.HistoryFilePath)
            };
            historyWindow.Show();
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.MainViewModel_OpenHistoryWindow_Error_opening_history_window___0_,
                ex.Message));
        }
    }

    #endregion

    #region Settings

    private async void OpenSettings(string? message = null)
    {
        try
        {
            if (message != null)
                ConfigViewModel.Instance.Message = message;

            var configWindow = new ConfigWindow
            {
                DataContext = ConfigViewModel.Instance
            };

            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow;

            if (mainWindow == null) return;
            await configWindow.ShowDialog(mainWindow);

            ConfigViewModel.Instance.Message = null;
            RefreshUi();
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.MainViewModel_OpenSettings_Error_opening_settings_window___0_, ex.Message));
        }
    }

    private void RefreshUi()
    {
        Thread.CurrentThread.CurrentCulture = ConfigViewModel.Instance.SelectedCulture
                                              ?? CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = ConfigViewModel.Instance.SelectedCulture
                                                ?? CultureInfo.InvariantCulture;

        Resources.Culture = ConfigViewModel.Instance.SelectedCulture;

        this.RaisePropertyChanged(string.Empty);

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        var mainWindow = desktop.MainWindow;
        mainWindow?.InvalidateVisual();
    }

    [GeneratedRegex(@"^[a-zA-Z]:/")]
    public static partial Regex MyRegex();

    #endregion

    #region Text

    public string MainWindowTitle => Resources.MainWindowTitle;
    public string ClearTextButton => Resources.ClearTextButton;
    public string ClearTextButtonAutomation => Resources.ClearTextButtonAutomation;
    public string SelectedPictogramHelpText => Resources.SelectedPictogramHelpText;

    public string TenseLabel => Resources.TenseLabel;
    public string PastTense => Resources.PastTense;
    public string PresentTense => Resources.PresentTense;
    public string FutureTense => Resources.FutureTense;

    public string PastTenseAutomation => Resources.PastTenseAutomation;
    public string PresentTenseAutomation => Resources.PresentTenseAutomation;
    public string FutureTenseAutomation => Resources.FutureTenseAutomation;

    public string FormLabel => Resources.FormLabel;
    public string DeclarativeForm => Resources.DeclarativeForm;
    public string DeclarativeFormAutomation => Resources.DeclarativeFormAutomation;
    public string QuestionForm => Resources.QuestionForm;
    public string QuestionFormAutomation => Resources.QuestionFormAutomation;

    // Other buttons
    public string BackButton => Resources.BackButton;
    public string BackButtonAutomation => Resources.BackButtonAutomation;
    public string HistoryButton => Resources.HistoryButton;
    public string HistoryButtonAutomation => Resources.HistoryButtonAutomation;
    public string SettingsButton => Resources.SettingsButton;
    public string SettingsButtonAutomation => Resources.SettingsButtonAutomation;

    // Navigation to previous/next boards
    public string PreviousBoard => Resources.PreviousBoard;
    public string PreviousBoardAutomation => Resources.PreviousBoardAutomation;
    public string NextBoard => Resources.NextBoard;
    public string NextBoardAutomation => Resources.NextBoardAutomation;

    // Loading indicator text
    public string LoadingText => Resources.LoadingText;

    // Call this after changing Resources.Culture to re-raise property changes
    public void RefreshLocalizedTexts()
    {
        // Using an empty string notifies the UI that *all* properties may have changed,
        // which triggers re-binding of every localized property.
        this.RaisePropertyChanged(string.Empty);
    }

    #endregion
}