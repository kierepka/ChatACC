using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ChatAAC.Models;

namespace ChatAAC.Services;

public class AiService
{
    private readonly OllamaClient _ollamaClient = new();
    private readonly ITtsService _ttsService = InitializeTtsService();


    private static ITtsService InitializeTtsService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacTtsService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsTtsService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxTtsService();

        throw new PlatformNotSupportedException("The platform is not supported for TTS.");
    }

    public async Task<string> GetAiResponseAsync(string prompt, string form, string tense)
    {
        var chatRequest = new ChatRequest
        {
            Prompt = prompt,
            Form = form,
            Tense = tense
        };

        var response = await _ollamaClient.ChatAsync(chatRequest);
        return await CombineResponseAsync(response);
    }

    public async Task SpeakResponseAsync(string response)
    {
        await _ttsService.SpeakAsync(response);
    }

    private static async Task<string> CombineResponseAsync(IAsyncEnumerable<string> responseStream)
    {
        var builder = new StringBuilder();
        await foreach (var chunk in responseStream)
            builder.Append(chunk);

        return builder.ToString();
    }
}