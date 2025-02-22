namespace ChatAAC.Abstractions.Services;

public interface ICachePathProvider
{
    /// <summary>
    /// Pobiera ścieżkę do katalogu cache plików OBF
    /// </summary>
    /// <returns>Ścieżka do katalogu cache OBF</returns>
    string GetObfCacheDirectory();

    /// <summary>
    /// Pobiera ścieżkę do katalogu cache piktogramów
    /// </summary>
    /// <returns>Ścieżka do katalogu cache piktogramów</returns>
    string GetPictogramsCacheDirectory();
}