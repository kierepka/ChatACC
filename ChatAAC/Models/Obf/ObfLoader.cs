using System.IO;
using System.Text.Json;

namespace ChatAAC.Models.Obf;


// Class for parsing OBF files
public class ObfLoader
{
    public static ObfFile? LoadObf(string filePath)
    {
        // Read JSON content from the file
        var jsonString = File.ReadAllText(filePath);
        // Deserialize the JSON into an ObfFile object
        var obfFile = JsonSerializer.Deserialize<ObfFile>(jsonString);
        return obfFile;
    }
}