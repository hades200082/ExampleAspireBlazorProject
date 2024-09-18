using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Application.Host.Api;

public static class JsonPatchInputFormatterProvider
{
    public static NewtonsoftJsonPatchInputFormatter GetInputFormatter()
    {
        var builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new UlidNewtonsoftJsonConverter());
            })
            .Services.BuildServiceProvider();

        return builder
            .GetRequiredService<IOptions<MvcOptions>>()
            .Value
            .InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>()
            .First();
    }

    public class UlidNewtonsoftJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Ulid ulid)
            {
                writer.WriteValue(ulid.ToString());
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value is not null)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var ulidString = (string) reader.Value;
                    if (Ulid.TryParse(ulidString, out var ulid))
                    {
                        return ulid;
                    }
                }
                else if (reader.TokenType == JsonToken.Bytes)
                {
                    var ulidBytes = (byte[]) reader.Value;
                    if (Ulid.TryParse(ulidBytes, out Ulid ulid))
                    {
                        return ulid;
                    }
                }
            }

            throw new JsonSerializationException("Invalid ULID value.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Ulid);
        }
    }
}