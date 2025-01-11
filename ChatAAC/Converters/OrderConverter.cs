using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatAAC.Converters;

/// <summary>
///     Custom JSON converter for a 2D array of nullable integers.
///     This converter is used to serialize and deserialize the 'order' property in a grid.
/// </summary>
public class OrderConverter : JsonConverter<int?[][]>
{
    /// <summary>
    ///     Reads and converts the JSON to a 2D array of nullable integers.
    /// </summary>
    /// <param name="reader">The Utf8JsonReader to read from.</param>
    /// <param name="typeToConvert">The type to convert to.</param>
    /// <param name="options">The JsonSerializerOptions to use.</param>
    /// <returns>A 2D array of nullable integers.</returns>
    public override int?[][] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Ensure the token is a start array
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected start of array");

        var result = new List<int?[]>();

        // Read each row of the array
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of inner array");

            var innerList = new List<int?>();

            // Read values in the array
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                        innerList.Add(null);
                        break;
                    case JsonTokenType.Number:
                        innerList.Add(reader.GetInt32());
                        break;
                    case JsonTokenType.String:
                        innerList.Add( int.Parse(reader.GetString() ?? "0"));
                        break;
                    case JsonTokenType.None:
                    case JsonTokenType.StartObject:
                    case JsonTokenType.EndObject:
                    case JsonTokenType.StartArray:
                    case JsonTokenType.EndArray:
                    case JsonTokenType.PropertyName:
                    case JsonTokenType.Comment:
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                    default:
                        throw new JsonException("Expected number or null");
                }
            }

            result.Add(innerList.ToArray());
        }

        return result.ToArray();
    }

    /// <summary>
    ///     Writes a 2D array of nullable integers as JSON.
    /// </summary>
    /// <param name="writer">The Utf8JsonWriter to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">The JsonSerializerOptions to use.</param>
    public override void Write(Utf8JsonWriter writer, int?[][] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var innerArray in value)
        {
            writer.WriteStartArray();

            foreach (var item in innerArray)
                if (item.HasValue)
                    writer.WriteNumberValue(item.Value);
                else
                    writer.WriteNullValue();

            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }
}