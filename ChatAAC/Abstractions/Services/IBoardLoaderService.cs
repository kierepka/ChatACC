// Abstractions/Services/IBoardLoaderService.cs

using System.Threading;
using System.Threading.Tasks;
using ChatAAC.Models.Obf;

namespace ChatAAC.Abstractions.Services;

public interface IBoardLoaderService
{
    /// <summary>
    /// Ładuje plik .obf lub .obz asynchronicznie
    /// </summary>
    /// <param name="filePath">Ścieżka do pliku</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Zadanie reprezentujące operację ładowania</returns>
    Task LoadObfOrObzFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ładuje plik .obf asynchronicznie
    /// </summary>
    /// <param name="filePath">Ścieżka do pliku .obf</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Zadanie reprezentujące operację ładowania</returns>
    Task LoadObfFileAsync(string? filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zapisuje plik .obf asynchronicznie
    /// </summary>
    /// <param name="filePath">Ścieżka docelowa pliku</param>
    /// <param name="obfFile">Obiekt pliku OBF do zapisu</param>
    /// <param name="cancellationToken">Token anulowania operacji</param>
    /// <returns>Zadanie reprezentujące operację zapisu</returns>
    Task SaveObfFileAsync(string filePath, ObfFile obfFile, CancellationToken cancellationToken = default);
}