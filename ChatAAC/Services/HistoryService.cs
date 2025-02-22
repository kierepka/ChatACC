using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models;

namespace ChatAAC.Services;

public class HistoryService
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    public ObservableCollection<AiResponse> HistoryItems { get; } = [];

    public string HistoryFilePath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatAAC", "ai_response_history.json");


    public async Task LoadHistoryAsync()
    {
        if (!File.Exists(HistoryFilePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(HistoryFilePath);
            var history = JsonSerializer.Deserialize<ObservableCollection<AiResponse>>(json, _jsonOptions);
            if (history is null) return;

            foreach (var item in history)
                HistoryItems.Add(item);

        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.HistoryService_LoadHistory_Error_loading_history___0_, ex.Message));
        }
    }

    public async Task SaveHistoryAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(HistoryFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(HistoryItems, _jsonOptions);
            await File.WriteAllTextAsync(HistoryFilePath, json);

        }
        catch (Exception ex)
        {
            AppLogger.LogError(string.Format(
                Resources.HistoryViewModel_SaveHistory_Error_saving_history___0_, ex.Message));
        }
    }

    public async Task AddToHistoryAsync(AiResponse response)
    {
        HistoryItems.Add(response);
        await SaveHistoryAsync();
    }
}