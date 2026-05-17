using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Mediator.Infrastructure.Impl;


/// <summary>
/// Default <see cref="ISerializerService"/> implementation backed by <c>System.Text.Json</c>. Replace via
/// <c>ShinyMediatorBuilder.SetSerializer</c> to plug in a different serializer (e.g. Newtonsoft, source-generated).
/// </summary>
public class SysTextJsonSerializerService : ISerializerService
{
    /// <summary>
    /// Options applied to every serialize/deserialize operation. Mutate to customize naming, converters, etc.
    /// </summary>
    public JsonSerializerOptions JsonOptions { get; set; } = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultBufferSize = 128
    };

    /// <inheritdoc/>
    public string Serialize<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, this.JsonOptions);
        return json;
    }

    /// <inheritdoc/>
    public T Deserialize<T>(string content)
    {
        var obj = JsonSerializer.Deserialize<T>(content, this.JsonOptions)!;
        return obj;
    }

    /// <inheritdoc/>
    public object Deserialize(string content, Type type)
    {
        var obj = JsonSerializer.Deserialize(content, type, this.JsonOptions)!;
        return obj;
    }


    /// <inheritdoc/>
    public IAsyncEnumerable<T> DeserlializeAsyncEnumerable<T>(Stream stream, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsyncEnumerable<T>(stream, JsonOptions, cancellationToken);
}
