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
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Button = ChatAAC.Models.Obf.Button;

namespace ChatAAC.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        #region Fields

        private readonly ITtsService _ttsService;
        private readonly AiInteractionService _aiInteractionService;
        private readonly BoardLoaderService _boardLoaderService;
        private readonly HistoryService _historyService;

        private string _selectedTense = "Teraźniejszy";
        private string _selectedForm = "Oznajmująca";
        private int _quantity = 1;
        private string _constructedSentence = string.Empty;
        private string _aiResponse = string.Empty;
        private bool _isLoading;
        private ObfFile? _obfData;
        private int _currentBoardIndex;
        private int _gridRows;
        private int _gridColumns;
        private bool _isInitialized;
        private bool _isEditMode;

        // Kolekcja komórek gridu – reprezentacja przycisków na planszy
        public ObservableCollection<GridCellViewModel> GridCells { get; } = new();

        // Historia plików (ścieżki do OBF/OBZ)
        private List<string?> _obfFileHistory = new();
        private int _currentHistoryIndex = -1;

        #endregion

        #region Properties

        public int CurrentHistoryIndex
        {
            get => _currentHistoryIndex;
            set => this.RaiseAndSetIfChanged(ref _currentHistoryIndex, value);
        }

        public List<string?> ObfFileHistory
        {
            get => _obfFileHistory;
            set => this.RaiseAndSetIfChanged(ref _obfFileHistory, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => this.RaiseAndSetIfChanged(ref _isEditMode, value);
        }

        public ObfFile? ObfData
        {
            get => _obfData;
            set => this.RaiseAndSetIfChanged(ref _obfData, value);
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

        private string ConstructedSentence
        {
            get => _constructedSentence;
            set => this.RaiseAndSetIfChanged(ref _constructedSentence, value);
        }

        public int CurrentBoardIndex
        {
            get => _currentBoardIndex;
            set => this.RaiseAndSetIfChanged(ref _currentBoardIndex, value);
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

        #endregion

        #region Commands

        public ReactiveCommand<Unit, Unit> ToggleEditModeCommand { get; }
        public ReactiveCommand<Unit, Unit> EditGridCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveBoardCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenHistoryCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectBoardAndLoadCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearSelectedCommand { get; }
        public ReactiveCommand<GridCellViewModel, Unit> CellClickedCommand { get; }
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

        #region Constructor

        public MainViewModel(ILogger<MainViewModel> logger
        )
        {
            // Load settings
            ConfigViewModel.Instance.ReloadSettings();

            // Initialize services
            _ttsService = TtsServiceFactory.CreateTtsService();
            _aiInteractionService = new AiInteractionService();
            _boardLoaderService = new BoardLoaderService(
                this,
                logger,
                new FileTypeValidator(), 
                new CachePathProvider()
            );
            _historyService = new HistoryService();

            // Initialize commands
            ToggleEditModeCommand = ReactiveCommand.Create(ToggleEditMode);
            EditGridCommand = ReactiveCommand.CreateFromTask(EditGridAsync);
            SaveBoardCommand = ReactiveCommand.CreateFromTask(SaveBoardAsync);
            OpenSettingsCommand = ReactiveCommand.Create(() => OpenSettings());
            SelectBoardAndLoadCommand = ReactiveCommand.CreateFromTask(SelectBoardAndLoadAsync);
            ClearSelectedCommand = ReactiveCommand.Create(ClearSelected);
            CellClickedCommand = ReactiveCommand.CreateFromTask<GridCellViewModel>(OnCellClickedAsync);
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

            // Update constructed sentence whenever grid cells change
            GridCells.CollectionChanged += (_, _) => UpdateConstructedSentence();

            _ = _historyService.LoadHistoryAsync();
            _ = LoadInitialFileAsync();
        }

        #endregion

        #region File Loading / Initialization

        private async Task SelectBoardAndLoadAsync()
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                    return;

                var popupVm = new BoardPathsViewModel(ConfigViewModel.Instance.BoardPaths);
                var popupWindow = new BoardPathsWindow { DataContext = popupVm };

                if (desktop.MainWindow != null)
                    await popupWindow.ShowDialog(desktop.MainWindow);

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
                    await Dispatcher.UIThread.InvokeAsync(() => OpenSettings(Resources.MainViewModel_PleaseSetDefaultBoard));
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError(string.Format(Resources.MainViewModel_LoadInitialFileAsync_Błąd_podczas_wczytywania_pliku_początkowego___0_, ex.Message));
            }
            finally
            {
                IsLoading = false;
                _isInitialized = true;
            }
        }

        private async Task SaveBoardAsync()
        {
            if (string.IsNullOrEmpty(CurrentObfFilePath) || ObfData == null)
            {
                AppLogger.LogInfo("No board file or ObfData is null - cannot save.");
                return;
            }
            await _boardLoaderService.SaveObfFileAsync(CurrentObfFilePath, ObfData);
            AppLogger.LogInfo("Board saved successfully.");
        }

        // Metoda wywoływana przez GridCellViewModel przy edycji komórki
        public void UpdateGridOrderForCell(int row, int col, string newButtonId)
        {
            if (ObfData?.Grid?.Order != null)
            {
                if (row < ObfData.Grid.Order.Length && col < ObfData.Grid.Order[row].Length)
                    ObfData.Grid.Order[row][col] = newButtonId;
            }
        }

        // Ładuje przyciski (tworzy GridCells) z pliku OBF
        public void LoadButtonsFromObfData(ObfFile obfFile)
        {
            GridCells.Clear();
            if (obfFile.Grid != null)
            {
                var rowIndex = 0;
                foreach (var row in obfFile.Grid.Order)
                {
                    var columnIndex = 0;
                    foreach (var buttonId in row)
                    {
                        var btn = string.IsNullOrEmpty(buttonId) ? null : obfFile.Buttons.FirstOrDefault(b => b.Id == buttonId);
                        GridCells.Add(new GridCellViewModel(rowIndex, columnIndex, btn, this));
                        columnIndex++;
                    }
                    rowIndex++;
                }
                GridRows = obfFile.Grid.Rows;
                GridColumns = obfFile.Grid.Columns;
            }
            else
            {
                var col = 0;
                foreach (var btn in obfFile.Buttons)
                {
                    GridCells.Add(new GridCellViewModel(0, col, btn, this));
                    col++;
                }
                GridRows = 1;
                GridColumns = obfFile.Buttons.Count;
            }
        }

        public void UpdateGridCells()
        {
            if (ObfData?.Grid != null)
            {
                GridCells.Clear();
                for (var r = 0; r < ObfData.Grid.Rows; r++)
                {
                    for (var c = 0; c < ObfData.Grid.Columns; c++)
                    {
                        var btnId = ObfData.Grid.Order[r][c];
                        var btn = string.IsNullOrEmpty(btnId) ? null : ObfData.Buttons.FirstOrDefault(b => b.Id == btnId);
                        GridCells.Add(new GridCellViewModel(r, c, btn, this));
                    }
                }
            }
            else if (ObfData != null)
            {
                GridCells.Clear();
                var col = 0;
                foreach (var btn in ObfData.Buttons)
                {
                    GridCells.Add(new GridCellViewModel(0, col, btn, this));
                    col++;
                }
                GridRows = 1;
                GridColumns = ObfData.Buttons.Count;
            }
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

        #region Methods – Edit Mode

        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            AppLogger.LogInfo($"Edit mode is now {(IsEditMode ? "ON" : "OFF")}");
        }

        private async Task EditGridAsync()
        {
            if (ObfData?.Grid == null)
            {
                AppLogger.LogInfo("No grid found in the loaded board.");
                return;
            }
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;
            var vm = new EditGridViewModel(ObfData.Grid);
            var win = new EditGridWindow { DataContext = vm };
            if (desktop.MainWindow != null)
                await win.ShowDialog(desktop.MainWindow);
            if (vm.IsConfirmed)
            {
                GridRows = ObfData.Grid.Rows;
                GridColumns = ObfData.Grid.Columns;
                UpdateGridCells();
                await SaveBoardAsync();
            }
        }

        public async Task EditButtonAsync(Button button)
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                    return;
                var editVm = new EditButtonViewModel(button, ObfData);
                var editWindow = new EditButtonWindow { DataContext = editVm };
                if (desktop.MainWindow != null)
                    await editWindow.ShowDialog(desktop.MainWindow);
                if (editVm.IsConfirmed)
                {
                    UpdateGridCells();
                    await SaveBoardAsync();
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error editing button: {ex.Message}");
            }
        }

        #endregion

        #region Cell Handling

        private async Task OnCellClickedAsync(GridCellViewModel cellVm)
        {
            if (IsEditMode)
            {
                if (cellVm.Button != null)
                {
                    await EditButtonAsync(cellVm.Button);
                }
                else
                {
                    var newButton = new Button
                    {
                        Id = Guid.NewGuid().ToString(),
                        Label = "New Button",
                        BorderColor = "#000000",
                        BackgroundColor = "#FFFFFF",
                        Vocalization = "",
                        Action = ""
                    };

                    if (ObfData != null)
                    {
                        ObfData.Buttons.Add(newButton);
                        cellVm.Button = newButton;
                        if (ObfData.Grid != null)
                        {
                            if (cellVm.Row < ObfData.Grid.Order.Length && cellVm.Column < ObfData.Grid.Order[cellVm.Row].Length)
                            {
                                ObfData.Grid.Order[cellVm.Row][cellVm.Column] = newButton.Id;
                            }
                        }
                    }
                    UpdateGridCells();
                    await SaveBoardAsync();
                }
            }
            else
            {
                // Tryb normalny – np. dodanie do SelectedButtons
            }
        }

        #endregion

        #region History

        private async Task AddAiResponseToHistory(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return;
            await _historyService.AddToHistoryAsync(new AiResponse(response));
            await _historyService.SaveHistoryAsync();
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
                AppLogger.LogError(string.Format(Resources.MainViewModel_OpenHistoryWindow_Error_opening_history_window___0_, ex.Message));
            }
        }

        #endregion

        #region Sentence Handling

        private void UpdateConstructedSentence()
        {
            // Zbieramy etykiety przycisków z GridCells, które nie są puste
            ConstructedSentence = string.Join(" ", GridCells.Where(cell => cell.Button != null)
                                                             .Select(cell => cell.Button?.Label));
            AppLogger.LogInfo(string.Format(Resources.MainViewModel_UpdateConstructedSentence_Constructed_sentence___0_, ConstructedSentence));
        }

        private void SpeakConstructedSentence()
        {
            var sentence = ConstructedSentence;
            Task.Run(async () =>
            {
                try
                {
                    await _ttsService.SpeakAsync(sentence).ConfigureAwait(false);
                    AppLogger.LogInfo(string.Format(Resources.MainViewModel_SpeakConstructedSentence_Spoken_sentence___0_, sentence));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError(string.Format(Resources.MainViewModel_SpeakConstructedSentence_Error_speaking_text___0_, ex.Message));
                }
            });
        }

        #endregion

        #region AI Interaction

        private async Task SendToAiAsync()
        {
            IsLoading = true;
            AiResponse = Resources.MainWindowGeneratingResponse;
            AppLogger.LogInfo(string.Format(Resources.MainViewModel_SendToAiAsync_Sending_query___0_, ConstructedSentence));
            var response = await _aiInteractionService.SendToAiAsync(
                ConstructedSentence,
                SelectedForm,
                SelectedTense,
                Quantity,
                AppLogger.LogInfo);
            AiResponse = response;
            AppLogger.LogInfo(string.Format(Resources.MainViewModel_SendToAiAsync_AI_Response___0_, AiResponse));
            IsLoading = false;
            await AddAiResponseToHistory(AiResponse);
            CopyAiResponseToClipboard();
            await SpeakAiResponseAsync();
        }

        private async Task SpeakAiResponseAsync()
        {
            if (!string.IsNullOrEmpty(AiResponse))
            {
                try
                {
                    await _ttsService.SpeakAsync(AiResponse).ConfigureAwait(false);
                    AppLogger.LogInfo(string.Format(Resources.MainViewModel_SpeakAiResponseAsync_Spoken_AI_response___0_, AiResponse));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError(string.Format(Resources.MainViewModel_SpeakAiResponseAsync_Error_speaking_AI_response___0_, ex.Message));
                }
            }
        }

        #endregion

        #region Clipboard Handling

        private void CopyAiResponseToClipboard()
        {
            if (!string.IsNullOrEmpty(AiResponse))
                ClipboardService.CopyToClipboard(AiResponse);
        }

        private void CopyConstructedSentenceToClipboard()
        {
            if (!string.IsNullOrEmpty(ConstructedSentence))
                ClipboardService.CopyToClipboard(ConstructedSentence);
        }

        #endregion

        #region Settings

        private async void OpenSettings(string? message = null)
        {
            try
            {
                if (message != null)
                    ConfigViewModel.Instance.Message = message;
                var configWindow = new ConfigWindow { DataContext = ConfigViewModel.Instance };
                var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (mainWindow == null)
                    return;
                await configWindow.ShowDialog(mainWindow);
                ConfigViewModel.Instance.Message = null;
                RefreshUi();
            }
            catch (Exception ex)
            {
                AppLogger.LogError(string.Format(Resources.MainViewModel_OpenSettings_Error_opening_settings_window___0_, ex.Message));
            }
        }

        private void RefreshUi()
        {
            Thread.CurrentThread.CurrentCulture = ConfigViewModel.Instance.SelectedCulture ?? CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = ConfigViewModel.Instance.SelectedCulture ?? CultureInfo.InvariantCulture;
            Resources.Culture = ConfigViewModel.Instance.SelectedCulture;
            this.RaisePropertyChanged(string.Empty);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                mainWindow?.InvalidateVisual();
            }
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
        public string BackButton => Resources.BackButton;
        public string BackButtonAutomation => Resources.BackButtonAutomation;
        public string HistoryButton => Resources.HistoryButton;
        public string HistoryButtonAutomation => Resources.HistoryButtonAutomation;
        public string SettingsButton => Resources.SettingsButton;
        public string SettingsButtonAutomation => Resources.SettingsButtonAutomation;
        public string PreviousBoard => Resources.PreviousBoard;
        public string PreviousBoardAutomation => Resources.PreviousBoardAutomation;
        public string NextBoard => Resources.NextBoard;
        public string NextBoardAutomation => Resources.NextBoardAutomation;
        public string LoadingText => Resources.LoadingText;

        public void RefreshLocalizedTexts()
        {
            this.RaisePropertyChanged(string.Empty);
        }

        #endregion

        #region Dodatkowe metody

        private void ClearSelected()
        {
            AppLogger.LogInfo("ClearSelected executed.");
        }

        #endregion
    }
}