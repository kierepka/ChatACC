using ChatAAC.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatAAC.ViewModels;
using OllamaSharp;

namespace ChatAAC.Services;

public class OllamaClient
{
    readonly Chat _chat;

    public OllamaClient()
    {
        
        Console.WriteLine($"Connecting to {ConfigViewModel.Instance.OllamaAddress} ...");

        var ollama = new OllamaApiClient(ConfigViewModel.Instance.OllamaAddress)
        {
            SelectedModel = ConfigViewModel.Instance.SelectedModel ?? "gemma2"
        };
        _chat = new Chat(ollama);
    }

    public Task<IAsyncEnumerable<string>> ChatAsync(ChatRequest request)
    {
        Console.WriteLine("Żądanie do Ollama:");
        
        
        var prompt = $@"Jesteś asystentem komunikacyjnym dla osoby niepełnosprawnej, która używa systemu AAC (Augmentative and Alternative Communication). Twoim zadaniem jest przekształcenie wybranych przez tę osobę słów kluczowych w jedno pełne, gramatycznie poprawne zdanie.

			Kontekst: Osoba niepełnosprawna wybrała następujące słowa kluczowe: [{request.Prompt}]

			Twoje zadanie:

			1.	Przeanalizuj podane słowa kluczowe.
			2.	Używając tych słów, stwórz jedno sensowne zdanie skierowane do jednej osoby.
			3.	Upewnij się, że zdanie jest gramatycznie poprawne i oddaje intencję komunikacyjną użytkownika.
			4.	Dodaj niezbędne słowa łączące lub kontekstowe, aby zdanie brzmiało naturalnie.
			5.	Nie zmieniaj znaczenia ani nie wprowadzaj informacji spoza podanych słów kluczowych.
			6.	Sformułuj zdanie w trybie {request.Form}.
			7.	Użyj czasu {request.Tense}.
			8.	Przyjmij, że jeśli występuje liczba, jest to: {request.Quantity}.
			9.	Zachowaj prostotę i klarowność wypowiedzi nie pomijając słów kluczowych.

			Odpowiedź:

			Podaj tylko jedno wygenerowane zdanie, bez żadnych dodatkowych komentarzy czy wyjaśnień.
			Przygotuj odpowiedź do odczytania dla osoby z którą rozmawia osoba niepełnosprawna w języku: {ConfigViewModel.Instance.SelectedLanguage}.";
		
        

        Console.WriteLine(prompt);
        

        return Task.FromResult(_chat.Send(prompt));
    }
}