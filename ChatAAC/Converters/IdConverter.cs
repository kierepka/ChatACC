using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatAAC.Converters;


/// <summary>
/// Custom JSON converter for handling 'id' values. Converts the value to an integer.
/// </summary>
public class IdConverter : JsonConverter<int>
{
    /// <summary>
    /// Reads and converts the JSON data to an integer.
    /// </summary>
    /// <param name="reader">The Utf8JsonReader to read from.</param>
    /// <param name="typeToConvert">The type to convert to. In this case, it's always int.</param>
    /// <param name="options">The JsonSerializerOptions to use during deserialization.</param>
    /// <returns>The converted integer value.</returns>
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If the value is a number, return it as an int
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        // If the value is a string, try to convert it to an integer
        else if (reader.TokenType == JsonTokenType.String)
        {
            if (int.TryParse(reader.GetString(), out var result))
            {
                return result;
            }
            throw new JsonException($"Unable to convert string to int: {reader.GetString()}");
        }
        else
        {
            throw new JsonException($"Unexpected token {reader.TokenType} when parsing id.");
        }
    }

    /// <summary>
    /// Writes the integer value as JSON data.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write to.</param>
    /// <param name="value">The integer value to write.</param>
    /// <param name="options">The JsonSerializerOptions to use during serialization.</param>
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // Write as an int
        writer.WriteNumberValue(value);
    }
}