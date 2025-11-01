using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Mediator.Infrastructure.Impl;


public class SysTextJsonSerializerService : ISerializerService
{
    public JsonSerializerOptions JsonOptions { get; set; } = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultBufferSize = 128
    };
    
    public string Serialize<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, this.JsonOptions);
        return json;
    }

    public T Deserialize<T>(string content)
    {
        var obj = JsonSerializer.Deserialize<T>(content, this.JsonOptions)!;
        return obj;
    }

    public object Deserialize(string content, Type type)
    {
        var obj = JsonSerializer.Deserialize(content, type, this.JsonOptions)!;
        return obj;
    }


    public IAsyncEnumerable<T> DeserlializeAsyncEnumerable<T>(Stream stream, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsyncEnumerable<T>(stream, JsonOptions, cancellationToken);
}