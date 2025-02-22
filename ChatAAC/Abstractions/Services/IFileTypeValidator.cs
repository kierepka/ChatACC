namespace ChatAAC.Abstractions.Services;

public interface IFileTypeValidator
{
    /// <summary>
    /// Sprawdza, czy plik jest obrazem
    /// </summary>
    /// <param name="fileName">Nazwa pliku</param>
    /// <returns>True, jeśli plik jest obrazem, False w przeciwnym razie</returns>
    bool IsImageFile(string fileName);

    /// <summary>
    /// Sprawdza, czy plik jest plikiem OBF
    /// </summary>
    /// <param name="fileName">Nazwa pliku</param>
    /// <returns>True, jeśli plik jest OBF, False w przeciwnym razie</returns>
    bool IsObfFile(string fileName);

    /// <summary>
    /// Sprawdza, czy plik jest plikiem manifestu
    /// </summary>
    /// <param name="fileName">Nazwa pliku</param>
    /// <returns>True, jeśli plik jest manifestem, False w przeciwnym razie</returns>
    bool IsManifestFile(string fileName);

    /// <summary>
    /// Sprawdza, czy plik jest plikiem OBZ
    /// </summary>
    /// <param name="fileName">Nazwa pliku</param>
    /// <returns>True, jeśli plik jest OBZ, False w przeciwnym razie</returns>
    bool IsObzFile(string fileName);
}