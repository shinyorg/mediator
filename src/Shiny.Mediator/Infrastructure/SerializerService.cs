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
    public SerializerService()
    {
        this.JsonOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }
    
    
    public JsonSerializerOptions JsonOptions { get; set; }
    
    public string Serialize<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, this.JsonOptions);
        return json;
    }

    public T Deserialize<T>(string json)
    {
        var obj = JsonSerializer.Deserialize<T>(json, this.JsonOptions)!;
        return obj;
    }
}