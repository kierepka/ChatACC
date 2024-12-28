using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using ChatAAC.Models;

public class HistoryService
{
    public ObservableCollection<AiResponse> HistoryItems { get; } = new();
    public string HistoryFilePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatAAC", "ai_response_history.json");

    public void LoadHistory()
    {
        if (!File.Exists(HistoryFilePath)) return;

        try
        {
            var json = File.ReadAllText(HistoryFilePath);
            var history = JsonSerializer.Deserialize<ObservableCollection<AiResponse>>(json);
            if (history != null)
                foreach (var item in history)
                    HistoryItems.Add(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading history: {ex.Message}");
        }
    }

    public void SaveHistory()
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

    public void AddToHistory(AiResponse response)
    {
        HistoryItems.Add(response);
        SaveHistory();
    }
}