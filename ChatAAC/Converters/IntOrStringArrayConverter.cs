using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatAAC.Converters;

public class IntOrStringArrayConverter : JsonConverter<string?[][]>
{
    public override string?[][] Read(ref Utf8JsonReader reader,
                                     Type typeToConvert,
                                     JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array.");

        var result = new List<List<string?>>();

        while (true)
        {
            if (!reader.Read())
                throw new JsonException("Unexpected end while reading grid array.");

            if (reader.TokenType == JsonTokenType.EndArray)
                break; // end of the outer array

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of inner array.");

            var innerList = new List<string?>();
            while (true)
            {
                if (!reader.Read())
                    throw new JsonException("Unexpected end while reading inner array.");

                if (reader.TokenType == JsonTokenType.EndArray)
                    break; // end of this inner array

                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                    {
                        var num = reader.GetInt32();
                        innerList.Add(num.ToString());
                        break;
                    }
                    case JsonTokenType.String:
                    {
                        var str = reader.GetString();
                        innerList.Add(str);
                        break;
                    }
                    case JsonTokenType.Null:
                    {
                        // Add an actual null to the list
                        innerList.Add(null);
                        break;
                    }
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
                        throw new JsonException($"Unsupported token {reader.TokenType} in inner array");
                }
            }
            result.Add(innerList);
        }

        return result.Select(lst => lst.ToArray()).ToArray();
    }

    public override void Write(Utf8JsonWriter writer,
                               string?[][]? value,
                               JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var inner in value)
        {
            writer.WriteStartArray();
            foreach (var item in inner)
            {
                if (int.TryParse(item, out var number))
                    writer.WriteNumberValue(number);
                else if (item == null)
                    writer.WriteNullValue();
                else
                    writer.WriteStringValue(item);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}