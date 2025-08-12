using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lucidly.Common
{
    public class PropertySchemaConverter : JsonConverter<PropertySchema>
    {
        public override PropertySchema? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Clone the reader so we can inspect the Type field
            var readerCopy = reader;

            using var jsonDoc = JsonDocument.ParseValue(ref readerCopy);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("Type", out var typeProperty))
                throw new JsonException("Missing 'Type' discriminator.");

            var typeDiscriminator = typeProperty.GetString();

            Type targetType = typeDiscriminator?.ToLowerInvariant() switch
            {
                "string" => typeof(StringPropertySchema),
                "number" => typeof(NumberPropertySchema),
                "boolean" => typeof(BooleanPropertySchema),
                "enum" => typeof(EnumPropertySchema),
                _ => throw new JsonException($"Unknown schema type: {typeDiscriminator}")
            };

            return (PropertySchema?)JsonSerializer.Deserialize(ref reader, targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, PropertySchema value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
