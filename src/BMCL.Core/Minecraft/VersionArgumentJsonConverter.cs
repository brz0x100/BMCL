using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMCL.Core.Minecraft;

internal sealed class VersionArgumentJsonConverter : JsonConverter<VersionArgument>
{
    public override VersionArgument Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return VersionArgument.FromValue(reader.GetString() ?? "");
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("A version argument must be a string or an object.");
        }

        IReadOnlyList<string> values = [];
        IReadOnlyList<Rule> rules = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Unexpected token inside a version argument.");
            }

            var propertyName = reader.GetString();
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of a version argument.");
            }

            switch (propertyName)
            {
                case "value":
                    values = ReadValues(ref reader);
                    break;
                case "rules":
                    rules = JsonSerializer.Deserialize<List<Rule>>(ref reader, options) ?? [];
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new VersionArgument(values, rules);
    }

    public override void Write(
        Utf8JsonWriter writer,
        VersionArgument value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Values.Count == 1)
        {
            writer.WriteString("value", value.Values[0]);
        }
        else
        {
            writer.WriteStartArray("value");
            foreach (var item in value.Values)
            {
                writer.WriteStringValue(item);
            }

            writer.WriteEndArray();
        }

        if (value.Rules.Count > 0)
        {
            writer.WritePropertyName("rules");
            JsonSerializer.Serialize(writer, value.Rules, options);
        }

        writer.WriteEndObject();
    }

    private static IReadOnlyList<string> ReadValues(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return [reader.GetString() ?? ""];
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("A version argument value must be a string or an array.");
        }

        var values = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                values.Add(reader.GetString() ?? "");
            }
        }

        return values;
    }
}
