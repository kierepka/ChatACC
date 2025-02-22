using System.Threading;
using System.Threading.Tasks;
using ChatAAC.Models.Obf;

namespace ChatAAC.Abstractions.Services;

public interface IObfLoader
{
    /// <summary>
    /// Ładuje plik OBF asynchronicznie
    /// </summary>
    /// <param name="filePath">Ścieżka do pliku</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Załadowany obiekt ObfFile lub null</returns>
    Task<ObfFile?> LoadObfAsync(string? filePath, CancellationToken cancellationToken = default);
}