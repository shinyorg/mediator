using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Built-in resolver shipped with Shiny.Mediator.AppSupport. Wires JsonTypeInfo for the
/// infrastructure wire-format envelopes (<see cref="InternalCacheEntry"/>,
/// <see cref="CacheItemConfig"/>, <see cref="OfflineStore"/>) once via hand-rolled converters so
/// the storage / cache / offline paths work AOT-clean for every consumer without requiring users
/// to declare these types in their own JsonSerializerContext.
/// </summary>
public sealed class AppSupportJsonResolver : IJsonTypeInfoResolver
{
    /// <summary>Process-wide singleton; registered with <c>Shiny.Json</c> via module init.</summary>
    public static readonly AppSupportJsonResolver Instance = new();

    AppSupportJsonResolver() { }

    /// <inheritdoc/>
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type == typeof(InternalCacheEntry))
            return JsonMetadataServices.CreateValueInfo<InternalCacheEntry>(options, new InternalCacheEntryConverter());
        if (type == typeof(CacheItemConfig))
            return JsonMetadataServices.CreateValueInfo<CacheItemConfig>(options, new CacheItemConfigConverter());
        if (type == typeof(OfflineStore))
            return JsonMetadataServices.CreateValueInfo<OfflineStore>(options, new OfflineStoreConverter());
        // AbstractFileStorageService persists its (category, key) → filename index as this nested
        // dict shape. Register it here so the strict chain can write the index without a separate
        // user opt-in — it's an infrastructure type users never see.
        if (type == typeof(ConcurrentDictionary<string, ConcurrentDictionary<string, string>>))
            return JsonMetadataServices.CreateValueInfo<ConcurrentDictionary<string, ConcurrentDictionary<string, string>>>(
                options,
                new StorageIndexConverter()
            );
        return null;
    }


    sealed class InternalCacheEntryConverter : JsonConverter<InternalCacheEntry>
    {
        public override InternalCacheEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string? key = null;
            string? valueJson = null;
            DateTimeOffset createdAt = default;
            DateTimeOffset? expiresAt = null;
            CacheItemConfig? config = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new InternalCacheEntry(key!, valueJson!, createdAt, expiresAt, config);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var prop = reader.GetString();
                reader.Read();

                switch (prop)
                {
                    case "Key":
                        key = reader.GetString();
                        break;
                    case "ValueJson":
                        valueJson = reader.GetString();
                        break;
                    case "CreatedAt":
                        createdAt = reader.GetDateTimeOffset();
                        break;
                    case "ExpiresAt":
                        expiresAt = reader.TokenType == JsonTokenType.Null ? null : reader.GetDateTimeOffset();
                        break;
                    case "Config":
                        config = reader.TokenType == JsonTokenType.Null
                            ? null
                            : ReadCacheItemConfig(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, InternalCacheEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Key", value.Key);
            writer.WriteString("ValueJson", value.ValueJson);
            writer.WriteString("CreatedAt", value.CreatedAt.ToString("O"));
            writer.WritePropertyName("ExpiresAt");
            if (value.ExpiresAt is null) writer.WriteNullValue();
            else writer.WriteStringValue(value.ExpiresAt.Value.ToString("O"));
            writer.WritePropertyName("Config");
            if (value.Config is null) writer.WriteNullValue();
            else WriteCacheItemConfig(writer, value.Config);
            writer.WriteEndObject();
        }
    }


    sealed class CacheItemConfigConverter : JsonConverter<CacheItemConfig>
    {
        public override CacheItemConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return ReadCacheItemConfig(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, CacheItemConfig value, JsonSerializerOptions options)
            => WriteCacheItemConfig(writer, value);
    }


    sealed class StorageIndexConverter : JsonConverter<ConcurrentDictionary<string, ConcurrentDictionary<string, string>>>
    {
        public override ConcurrentDictionary<string, ConcurrentDictionary<string, string>>? Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            var outer = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return outer;
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();

                var category = reader.GetString()!;
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null) { outer[category] = new(); continue; }
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

                var inner = new ConcurrentDictionary<string, string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                    var key = reader.GetString()!;
                    reader.Read();
                    inner[key] = reader.GetString() ?? string.Empty;
                }
                outer[category] = inner;
            }
            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            ConcurrentDictionary<string, ConcurrentDictionary<string, string>> value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            foreach (var (category, inner) in value)
            {
                writer.WritePropertyName(category);
                writer.WriteStartObject();
                foreach (var (k, v) in inner)
                    writer.WriteString(k, v);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
    }


    sealed class OfflineStoreConverter : JsonConverter<OfflineStore>
    {
        public override OfflineStore? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            string? typeName = null;
            string? requestKey = null;
            DateTimeOffset timestamp = default;
            string? json = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new OfflineStore(typeName!, requestKey!, timestamp, json!);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var prop = reader.GetString();
                reader.Read();

                switch (prop)
                {
                    case "TypeName": typeName = reader.GetString(); break;
                    case "RequestKey": requestKey = reader.GetString(); break;
                    case "Timestamp": timestamp = reader.GetDateTimeOffset(); break;
                    case "Json": json = reader.GetString(); break;
                    default: reader.Skip(); break;
                }
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, OfflineStore value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("TypeName", value.TypeName);
            writer.WriteString("RequestKey", value.RequestKey);
            writer.WriteString("Timestamp", value.Timestamp.ToString("O"));
            writer.WriteString("Json", value.Json);
            writer.WriteEndObject();
        }
    }


    // CacheItemConfig is embedded inside InternalCacheEntry as well — share the inline reader/writer.
    static CacheItemConfig ReadCacheItemConfig(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        TimeSpan? absolute = null;
        TimeSpan? sliding = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new CacheItemConfig(absolute, sliding);

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var prop = reader.GetString();
            reader.Read();

            switch (prop)
            {
                case "AbsoluteExpiration":
                    absolute = reader.TokenType == JsonTokenType.Null ? null : TimeSpan.Parse(reader.GetString()!);
                    break;
                case "SlidingExpiration":
                    sliding = reader.TokenType == JsonTokenType.Null ? null : TimeSpan.Parse(reader.GetString()!);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }
        throw new JsonException();
    }

    static void WriteCacheItemConfig(Utf8JsonWriter writer, CacheItemConfig value)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("AbsoluteExpiration");
        if (value.AbsoluteExpiration is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.AbsoluteExpiration.Value.ToString("c"));
        writer.WritePropertyName("SlidingExpiration");
        if (value.SlidingExpiration is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.SlidingExpiration.Value.ToString("c"));
        writer.WriteEndObject();
    }
}


/// <summary>
/// Module-init shim that registers <see cref="AppSupportJsonResolver"/> with
/// <see cref="Shiny.Json"/> before <c>Main</c> runs.
/// </summary>
internal static class AppSupportJsonResolverInit
{
#pragma warning disable CA2255 // ModuleInitializer is the intended hook for chain auto-registration; same pattern used by Shiny.Extensions.Serialization
    [ModuleInitializer]
    internal static void Init() => Shiny.Json.AddResolver(AppSupportJsonResolver.Instance);
#pragma warning restore CA2255
}
