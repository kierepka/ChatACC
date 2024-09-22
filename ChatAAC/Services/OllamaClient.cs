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
        
        
        string prompt = $@"Jesteś asystentem komunikacyjnym dla osoby niepełnosprawnej, która używa systemu AAC (Augmentative and Alternative Communication). Twoim zadaniem jest przekształcenie wybranych przez tę osobę słów kluczowych w pełne, gramatycznie poprawne zdanie lub krótką wypowiedź.

            Kontekst: Osoba niepełnosprawna wybrała następujące słowa kluczowe: [{request.Prompt}]

            Twoje zadanie:
            1. Przeanalizuj podane słowa kluczowe.
            2. Stwórz z nich jedno pełne, sensowne zdanie lub krótką wypowiedź.
            3. Upewnij się, że zdanie jest gramatycznie poprawne i zachowuje intencję komunikacyjną użytkownika.
            4. Jeśli to konieczne, dodaj odpowiednie słowa łączące lub kontekstowe, aby zdanie brzmiało naturalnie.
            5. Nie zmieniaj znaczenia ani nie dodawaj nowych informacji, których nie ma w oryginalnych słowach kluczowych.
            6. Jeśli słowa kluczowe sugerują pytanie, sformułuj je jako pytanie.
            7. Zachowaj prostotę wypowiedzi, unikając zbyt skomplikowanych konstrukcji.

            Odpowiedź:
            Podaj tylko wygenerowane zdanie lub krótką wypowiedź, bez żadnych dodatkowych komentarzy czy wyjaśnień.";

        Console.WriteLine(prompt);
        

        return Task.FromResult(_chat.Send(prompt));
    }
}