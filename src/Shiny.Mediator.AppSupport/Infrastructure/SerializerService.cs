using System.Text.Json;

namespace Shiny.Mediator.Infrastructure;

public interface ISerializerService
{
    string Serialize<T>(T obj);
    T Deserialize<T>(string json);
}

public class SerializerService : ISerializerService
{
    public string Serialize<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return json;
    }

    public T Deserialize<T>(string json)
    {
        var obj = JsonSerializer.Deserialize<T>(json)!;
        return obj;
    }
}