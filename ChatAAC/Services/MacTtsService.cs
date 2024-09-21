using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ChatAAC.Services;

public class MacTtsService : ITtsService
{
    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Tekst do odczytania nie może być pusty.", nameof(text));

        if (!IsMacOS())
            throw new PlatformNotSupportedException("TTS za pomocą 'say' jest wspierane tylko na macOS.");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "say",
            Arguments = $"\"{text}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process
            {
                StartInfo = processStartInfo
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Błąd TTS: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas odczytywania tekstu: {ex.Message}");
            throw;
        }
    }

    private bool IsMacOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}