using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ReactiveUI;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAAC.Models;
using ChatAAC.Services;

namespace ChatAAC.ViewModels
{
   public class HistoryViewModel : ReactiveObject
    {
        public ObservableCollection<AiResponse> HistoryItems { get; }
        public AiResponse? SelectedHistoryItem { get; set; }

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

        private string HistoryFilePath { get; set; }

        public ReactiveCommand<Unit, Unit> SortNewestToOldestCommand { get; }
        public ReactiveCommand<Unit, Unit> SortOldestToNewestCommand { get; }
        public ReactiveCommand<Unit, Unit> SortFavoritesCommand { get; }
        public ReactiveCommand<AiResponse, Unit> ToggleFavoriteCommand { get; }
        public ReactiveCommand<Unit, Task> SpeakSelectedEntryCommand { get; }
        public ReactiveCommand<AiResponse, Unit> SelectionChangedCommand { get; }
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
            var sorted = HistoryItems.OrderByDescending(item => item.IsFavorite).ThenByDescending(item => item.Timestamp).ToList();
            HistoryItems.Clear();
            foreach (var item in sorted)
                HistoryItems.Add(item);
        }

        private void ToggleFavorite(AiResponse item)
        {
            item.IsFavorite = !item.IsFavorite; // Toggle favorite status
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
                Console.WriteLine($"Error saving history: {ex.Message}");
            }
        }
        private async Task OnSelectionChanged(AiResponse item)
        {
            await SpeakSelectedEntry(); // Speak the selected item when changed
        }
        private async Task SpeakSelectedEntry()
        {
            if (SelectedHistoryItem == null)
                return;

            // Implement TTS for the selected history item
            var ttsService = new TtsService(); // Replace with actual TTS service
            await ttsService.SpeakAsync(SelectedHistoryItem.ResponseText);
        }
    }
}