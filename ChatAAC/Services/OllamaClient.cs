using ChatAAC.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OllamaSharp;

namespace ChatAAC.Services;

public class OllamaClient
{
    OllamaApiClient? ollama;
    Chat chat;
    private bool _connected = false;
    private readonly string _apiUrl;

    public OllamaClient(string apiUrl)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            apiUrl = "http://localhost:11434";

        if (!apiUrl.StartsWith("http"))
            apiUrl = "http://" + apiUrl;

        if (apiUrl.IndexOf(':', 5) < 0)
            apiUrl += ":11434";

        var uri = new Uri(apiUrl);

        Console.WriteLine($"Connecting to {uri} ...");

        ollama = new OllamaApiClient(apiUrl)
        {
            SelectedModel = "gemma2"
        };
        chat = new Chat(ollama);
    }

    public Task<IAsyncEnumerable<string>> ChatAsync(ChatRequest request)
    {
        Console.WriteLine("Żądanie do Ollama:");
        var prompt = $"z wybranych przez niepełnosprawnego słów [{request.Prompt}] utwórz z tego jedno pełne zdanie jako odpowiedź.";
        Console.WriteLine(prompt);
        

        return Task.FromResult(chat.Send(prompt));
    }
}