using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatAAC.Converters;

public class IntOrStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            // If the token is a number, convert it to string
            case JsonTokenType.Number:
            {
                // Get the integer value and convert to string
                var number = reader.GetInt32();
                return number.ToString();
            }
            // If the token is a string, just read it
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.None:
            case JsonTokenType.StartObject:
            case JsonTokenType.EndObject:
            case JsonTokenType.StartArray:
            case JsonTokenType.EndArray:
            case JsonTokenType.PropertyName:
            case JsonTokenType.Comment:
            case JsonTokenType.True:
            case JsonTokenType.False:
            case JsonTokenType.Null:
            default:
                throw new JsonException($"Unsupported token {reader.TokenType} for IntOrStringConverter");
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        // If value can be parsed as int, write as number; otherwise, write as string
        if (int.TryParse(value, out var number))
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}