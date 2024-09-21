using System;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace ChatAAC.Services;

public class WindowsTtsService : ITtsService
{
    private readonly SpeechSynthesizer _synthesizer = new();

    public Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Tekst do odczytania nie może być pusty.", nameof(text));

        _synthesizer.SpeakAsync(text);
        return Task.CompletedTask;
    }
}