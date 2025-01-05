using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ChatAAC.Helpers;
using ChatAAC.Lang;

namespace ChatAAC.Services;

public class MacTtsService : ITtsService
{
    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(Resources.MacTtsService_SpeakAsync_Tekst_do_odczytania_nie_może_być_pusty_,
                nameof(text));

        if (!IsMacOs())
            throw new PlatformNotSupportedException(Resources.MacTtsService_SpeakAsync_NotSupported);

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
            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0) throw new InvalidOperationException(Resources.MacTtsService_SpeakAsync_ErrorTts);
        }
        catch (Exception ex)
        {
            AppLogger.LogError(
                string.Format(Resources.MacTtsService_SpeakAsync_Błąd_podczas_odczytywania_tekstu___0_, ex.Message
                )
            );

            throw;
        }
    }

    private static bool IsMacOs()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}