using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatAAC.Converters;

public class IntFromStringConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetInt32();
                case JsonTokenType.String:
                {
                    var stringValue = reader.GetString();
                    if (int.TryParse(stringValue, out int result))
                    {
                        return result;
                    }

                    break;
                }
                default:
                    return 0;
            }
        }
        catch
        {
            // Ignorujemy wyjątki i przechodzimy do zwrócenia wartości domyślnej
        }

        // Jeśli nie udało się sparsować, zwracamy domyślną wartość (np. 0) lub rzucamy wyjątek
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}