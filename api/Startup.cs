using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using api;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[assembly: FunctionsStartup(typeof(Startup))]

namespace api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                settings.Converters.Add(new ChangeReasonConverter());
                settings.Converters.Add(new OptionalConverterFactory());
                return settings;
            };

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

                var cosmosClientOptions = new CosmosClientOptions()
                {
                    HttpClientFactory = httpClientFactory.CreateClient,
                    ConnectionMode = ConnectionMode.Gateway
                };

                return new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnectionString"), cosmosClientOptions);
            });
        }
    }



    class ChangeReasonConverter : JsonConverter<ChangeReason>
    {
        private readonly StringEnumConverter _converter;

        public ChangeReasonConverter()
        {
            _converter = new StringEnumConverter(true);
        }

        public override ChangeReason ReadJson(JsonReader reader, Type objectType, ChangeReason existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return (ChangeReason)_converter.ReadJson(reader, objectType, existingValue, serializer);
        }

        public override void WriteJson(JsonWriter writer, ChangeReason value, JsonSerializer serializer)
        {
            _converter.WriteJson(writer, value, serializer);
        }
    }

    class OptionalConverterFactory : JsonConverter
    {
        private ConcurrentDictionary<Type, JsonConverter> _converters = new ConcurrentDictionary<Type, JsonConverter>();
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType &&
                   typeof(Optional<>).IsAssignableFrom(typeToConvert.GetGenericTypeDefinition());
        }

        private JsonConverter GetConverter(Type objectType)
        {
            if (!_converters.TryGetValue(objectType, out var converter))
            {
                converter = (JsonConverter)Activator.CreateInstance(typeof(OptionalConverter<>).MakeGenericType(objectType.GetGenericArguments()[0]));
                _converters.TryAdd(objectType, converter);
            }
            return converter;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return GetConverter(objectType).ReadJson(reader, objectType, existingValue, serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            GetConverter(value.GetType()).WriteJson(writer, value, serializer);
        }
    }

    class OptionalConverter<T> : JsonConverter<Optional<T>>
    {
        public override Optional<T> ReadJson(JsonReader reader, Type objectType, Optional<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.Undefined)
            {
                return Optional<T>.None;
            }
            return serializer.Deserialize<T>(reader);
        }

        public override void WriteJson(JsonWriter writer, Optional<T> value, JsonSerializer serializer)
        {
            if (value.HasValue)
            {
                serializer.Serialize(writer, value.Value);
            }
            else
            {
                writer.WriteNull();
            }
        }
    }
}