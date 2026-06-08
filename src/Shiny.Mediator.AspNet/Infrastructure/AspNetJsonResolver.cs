using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Built-in resolver shipped with Shiny.Mediator.AspNet. Wires JsonTypeInfo for the
/// AspNet-side wire-format types (currently <see cref="ValidateResult"/>) via hand-rolled
/// converters so the validation exception handler can write its response under the strict chain
/// without requiring users to declare these types in their own JsonSerializerContext.
/// </summary>
public sealed class AspNetJsonResolver : IJsonTypeInfoResolver
{
    /// <summary>Process-wide singleton; registered with <c>Shiny.Json</c> via module init.</summary>
    public static readonly AspNetJsonResolver Instance = new();

    AspNetJsonResolver() { }

    /// <inheritdoc/>
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type == typeof(ValidateResult))
            return JsonMetadataServices.CreateValueInfo<ValidateResult>(options, new ValidateResultConverter());
        return null;
    }


    // ValidateResult shape: { "errors": { "PropName": ["error1", "error2"], ... } }
    // Kept stable for the lifetime of the wire format — clients on older app versions may parse this.
    sealed class ValidateResultConverter : JsonConverter<ValidateResult>
    {
        public override ValidateResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            var errors = new Dictionary<string, IReadOnlyList<string>>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new ValidateResult(errors);

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                var top = reader.GetString();
                reader.Read();

                if (top == "Errors" && reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject) break;
                        if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                        var propName = reader.GetString()!;
                        reader.Read();
                        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
                        var messages = new List<string>();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            messages.Add(reader.GetString() ?? string.Empty);
                        }
                        errors[propName] = messages;
                    }
                }
                else
                {
                    reader.Skip();
                }
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, ValidateResult value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Errors");
            writer.WriteStartObject();
            foreach (var (propName, messages) in value.Errors)
            {
                writer.WritePropertyName(propName);
                writer.WriteStartArray();
                foreach (var msg in messages)
                    writer.WriteStringValue(msg);
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}


internal static class AspNetJsonResolverInit
{
#pragma warning disable CA2255 // ModuleInitializer is the intended hook for chain auto-registration; same pattern used by Shiny.Extensions.Serialization and AppSupportJsonResolver
    [ModuleInitializer]
    internal static void Init() => Shiny.Json.AddResolver(AspNetJsonResolver.Instance);
#pragma warning restore CA2255
}
