using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Datastore.Converters
{
    /* See https://github.com/dotnet/runtime/issues/1197
       Can be removed once we switch to .NET 5
       Based on https://github.com/layomia/dotnet_runtime/blob/master/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/KeyValuePairConverter.cs
       and https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to
    */
    need to be removed ?

    public class KeyValuePairConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            if (typeToConvert.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
            {
                return false;
            }

            return true;
        }

        public override JsonConverter CreateConverter(
            Type type,
            JsonSerializerOptions options)
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];

            var converter = (JsonConverter)Activator.CreateInstance(
                typeof(KeyValuePairConverterInner<,>).MakeGenericType(
                    new Type[] { keyType, valueType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null);

            return converter;
        }

        private class KeyValuePairConverterInner<TKey, TValue> :
            JsonConverter<KeyValuePair<TKey, TValue>>
        {
            public override KeyValuePair<TKey, TValue> Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                TKey k = default;
                var keySet = false;

                TValue v = default;
                var valueSet = false;

                reader.Read();

                // Get the first property.
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                var propertyName = reader.GetString();
                if (string.Equals(propertyName, "Key", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    k = JsonSerializer.Deserialize<TKey>(ref reader, options);
                    keySet = true;
                }
                else if (string.Equals(propertyName, "Value", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    v = JsonSerializer.Deserialize<TValue>(ref reader, options);
                    valueSet = true;
                }
                else
                {
                    throw new JsonException();
                }

                // Get the second property.
                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                propertyName = reader.GetString();
                if (!keySet && string.Equals(propertyName, "Key", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    k = JsonSerializer.Deserialize<TKey>(ref reader, options);
                }
                else if (!valueSet && string.Equals(propertyName, "Value", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    v = JsonSerializer.Deserialize<TValue>(ref reader, options);
                }
                else
                {
                    throw new JsonException();
                }

                reader.Read();

                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }

                return new KeyValuePair<TKey, TValue>(k, v);
            }

            public override void Write(
                Utf8JsonWriter writer,
                KeyValuePair<TKey, TValue> kvp,
                JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("key");
                JsonSerializer.Serialize(writer, kvp.Key, options);

                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, kvp.Value, options);

                writer.WriteEndObject();
            }
        }
    }
}
