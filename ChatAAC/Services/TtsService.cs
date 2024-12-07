using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ChatAAC.Services;

public class TtsService
{
    /// <summary>
    ///     Wywołuje polecenie 'say' na macOS, aby odczytać podany tekst.
    /// </summary>
    /// <param name="text">Tekst do odczytania.</param>
    /// <returns>Zadanie reprezentujące operację asynchroniczną.</returns>
    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Tekst do odczytania nie może być pusty.", nameof(text));

        if (!IsMacOs())
            throw new PlatformNotSupportedException("TTS za pomocą 'say' jest wspierane tylko na macOS.");

        // Przygotowanie procesu
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

            // Opcjonalnie: Możesz odczytać wyjście lub błędy
            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0) throw new InvalidOperationException($"Błąd TTS: {error}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas odczytywania tekstu: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Sprawdza, czy aplikacja działa na macOS.
    /// </summary>
    /// <returns>Prawda, jeśli system operacyjny to macOS; w przeciwnym razie fałsz.</returns>
    private bool IsMacOs()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}