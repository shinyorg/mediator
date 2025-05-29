namespace Shiny.Mediator.Infrastructure;

public interface ISerializerService
{
    string Serialize<T>(T obj);
    T Deserialize<T>(string content);
    object Deserialize(string content, Type type);
}

