using System;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using ChatAAC.Lang;

namespace ChatAAC.Services;

[SupportedOSPlatform("windows")]
public class WindowsTtsService : ITtsService
{
    private readonly SpeechSynthesizer _synthesizer = new();

    public Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(
                string.Format(
                    Resources.MacTtsService_SpeakAsync_Tekst_do_odczytania_nie_może_być_pusty_
                    , nameof(text)));

        _synthesizer.SpeakAsync(text);
        return Task.CompletedTask;
    }
}