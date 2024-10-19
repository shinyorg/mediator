using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Mediator.Infrastructure;

public interface ISerializerService
{
    string Serialize<T>(T obj);
    T Deserialize<T>(string json);
}

public class SerializerService : ISerializerService
{
    readonly JsonSerializerOptions jsonOptions;

    public SerializerService(JsonSerializerOptions? options = null)
    {
        this.jsonOptions = options ?? new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }
    
    public string Serialize<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, this.jsonOptions);
        return json;
    }

    public T Deserialize<T>(string json)
    {
        var obj = JsonSerializer.Deserialize<T>(json, this.jsonOptions)!;
        return obj;
    }
}