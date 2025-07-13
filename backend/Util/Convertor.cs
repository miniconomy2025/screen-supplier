using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FlexibleStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If it's a string, return it directly
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        // If it's a number, convert it to string
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing a string.");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}