using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models;
using ChatAAC.Services;
using ReactiveUI;

namespace ChatAAC.ViewModels;

public class HistoryViewModel : ReactiveObject
{
    public HistoryViewModel(ObservableCollection<AiResponse> historyItems, string historyPath)
    {
        HistoryItems = historyItems;
        HistoryFilePath = historyPath;

        SortNewestToOldestCommand = ReactiveCommand.Create(SortNewestToOldest);
        SortOldestToNewestCommand = ReactiveCommand.Create(SortOldestToNewest);
        SortFavoritesCommand = ReactiveCommand.Create(SortFavorites);
        ToggleFavoriteCommand = ReactiveCommand.Create<AiResponse>(ToggleFavorite);
        SpeakSelectedEntryCommand = ReactiveCommand.Create(SpeakSelectedEntry);
        SelectionChangedCommand = ReactiveCommand.CreateFromTask<AiResponse>(OnSelectionChanged);
    }

    // Collection of AI responses displayed in the HistoryWindow
    public ObservableCollection<AiResponse> HistoryItems { get; }
    public AiResponse? SelectedHistoryItem { get; set; }

    // The file path where history is stored/loaded from
    private string HistoryFilePath { get; }

    // Commands for sorting, toggling favorites, speaking entries, etc.
    public ReactiveCommand<Unit, Unit> SortNewestToOldestCommand { get; }
    public ReactiveCommand<Unit, Unit> SortOldestToNewestCommand { get; }
    public ReactiveCommand<Unit, Unit> SortFavoritesCommand { get; }
    public ReactiveCommand<AiResponse, Unit> ToggleFavoriteCommand { get; }
    public ReactiveCommand<Unit, Task> SpeakSelectedEntryCommand { get; }
    public ReactiveCommand<AiResponse, Unit> SelectionChangedCommand { get; }

    #region Localized Properties

    public string HistoryWindowTitle => Resources.HistoryWindowTitle;
    public string SortNewestToOldestButton => Resources.SortNewestToOldestButton;
    public string SortOldestToNewestButton => Resources.SortOldestToNewestButton;
    public string SortFavoritesButton => Resources.SortFavoritesButton;
    public string SpeakSelectedEntryButton => Resources.SpeakSelectedEntryButton;

    // If you want to refresh these dynamically after changing Lang.Resources.Culture:
    public void RefreshLocalizedTexts()
    {
        // This method re-raises property changed notifications for *all* properties,
        // causing the UI to re-bind their values from the current culture.
        this.RaisePropertyChanged(string.Empty);
    }

    #endregion

    #region Sorting / Favorites / Saving Logic

    private void SortNewestToOldest()
    {
        var sorted = HistoryItems.OrderByDescending(item => item.Timestamp).ToList();
        HistoryItems.Clear();
        foreach (var item in sorted)
            HistoryItems.Add(item);
    }

    private void SortOldestToNewest()
    {
        var sorted = HistoryItems.OrderBy(item => item.Timestamp).ToList();
        HistoryItems.Clear();
        foreach (var item in sorted)
            HistoryItems.Add(item);
    }

    private void SortFavorites()
    {
        var sorted = HistoryItems
            .OrderByDescending(item => item.IsFavorite)
            .ThenByDescending(item => item.Timestamp)
            .ToList();
        HistoryItems.Clear();
        foreach (var item in sorted)
            HistoryItems.Add(item);
    }

    private void ToggleFavorite(AiResponse item)
    {
        item.IsFavorite = !item.IsFavorite;
        SaveHistory();
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(HistoryFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(HistoryItems, options);
            File.WriteAllText(HistoryFilePath, json);
        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.HistoryViewModel_SaveHistory_Error_saving_history___0_, ex.Message));
        }
    }

    private async Task OnSelectionChanged(AiResponse item)
    {
        await SpeakSelectedEntry(); // Speak the selected item automatically
    }

    private async Task SpeakSelectedEntry()
    {
        if (SelectedHistoryItem == null)
            return;

        // TTS for the selected history item
        var ttsService = TtsServiceFactory.CreateTtsService();
        await ttsService.SpeakAsync(SelectedHistoryItem.ResponseText);
    }

    #endregion
}