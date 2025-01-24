using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Mediator.Infrastructure.Impl;


public class SysTextJsonSerializerService : ISerializerService
{
    public JsonSerializerOptions JsonOptions { get; set; } = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    
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