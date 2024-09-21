using ChatAAC.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaSharp;

namespace ChatAAC.Services;

public class OllamaClient
{
    readonly Chat _chat;

    public OllamaClient(string apiUrl)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            apiUrl = "http://localhost:11434";

        if (!apiUrl.StartsWith("http"))
            apiUrl = "http://" + apiUrl;

        if (apiUrl.IndexOf(':', 5) < 0)
            apiUrl += ":11434";


        Console.WriteLine($"Connecting to {apiUrl} ...");

        var ollama = new OllamaApiClient(apiUrl)
        {
            SelectedModel = "gemma2"
        };
        _chat = new Chat(ollama);
    }

    public Task<IAsyncEnumerable<string>> ChatAsync(ChatRequest request)
    {
        Console.WriteLine("Żądanie do Ollama:");
        var prompt = $"z wybranych przez niepełnosprawnego słów [{request.Prompt}] utwórz z tego jedno pełne zdanie jako odpowiedź. Nie dodawaj własnych komentarzy dodatkowych.";
        Console.WriteLine(prompt);
        

        return Task.FromResult(_chat.Send(prompt));
    }
}