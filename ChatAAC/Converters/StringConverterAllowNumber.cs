using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatAAC.Converters;

public class StringConverterAllowNumber : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            // Obsługa wartości null
            JsonTokenType.Null => null,
            // Obsługa ciągów znaków
            JsonTokenType.String => reader.GetString(),
            // Obsługa liczb
            JsonTokenType.Number => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            // Obsługa wartości logicznych
            JsonTokenType.True or JsonTokenType.False => reader.GetBoolean().ToString(),
            _ => throw new JsonException(
                $"Nieoczekiwany typ tokenu {reader.TokenType} podczas parsowania ciągu znaków.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}