using System.Text.Json;
using Shiny.Impl;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Runtime-typed serialization helpers for mediator infrastructure that genuinely needs to dispatch
/// by <see cref="Type"/> at runtime (scheduled-command rehydration is the only current consumer).
/// These paths are not exposed on <see cref="Shiny.ISerializer"/> because they cannot be statically
/// verified by the trim/AOT analyzer — callers must ensure <paramref name="type"/> is registered in
/// a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
/// </summary>
public static class SerializerRuntimeExtensions
{
    /// <summary>Serializes <paramref name="value"/> using the JsonTypeInfo registered for <paramref name="type"/>.</summary>
    public static string Serialize(this Shiny.ISerializer serializer, object value, Type type)
    {
        var typeInfo = GetOptions(serializer).GetTypeInfo(type);
        return JsonSerializer.Serialize(value, typeInfo);
    }


    /// <summary>Deserializes <paramref name="content"/> into <paramref name="type"/> using its registered JsonTypeInfo.</summary>
    public static object Deserialize(this Shiny.ISerializer serializer, string content, Type type)
    {
        var typeInfo = GetOptions(serializer).GetTypeInfo(type);
        return JsonSerializer.Deserialize(content, typeInfo)!;
    }


    static JsonSerializerOptions GetOptions(Shiny.ISerializer serializer)
    {
        if (serializer is not DefaultJsonSerializer impl)
            throw new InvalidOperationException(
                $"Runtime-typed (Type-based) serialization requires Shiny.Impl.DefaultJsonSerializer; got '{serializer.GetType().FullName}'. " +
                $"Replace via Shiny.Json.AddContext/AddResolver instead of swapping the ISerializer implementation, or override the calling component."
            );
        return impl.Options;
    }
}
