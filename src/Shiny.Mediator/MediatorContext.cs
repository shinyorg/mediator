namespace Shiny.Mediator;

public class MediatorContext(object message, object messageHandler)
{
    public Guid Id { get; } = Guid.NewGuid();

    public object Message => message;
    public object MessageHandler => messageHandler;
    readonly Dictionary<string, object> store = new();
    public IReadOnlyDictionary<string, object> Values => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);
    
    
    public MediatorContext PopulateHeaders(
        IEnumerable<(string Key, object Value)> headers
    )
    {
        foreach (var header in headers)
            this.Add(header.Key, header.Value);
        
        return this;
    }
    
    public T? TryGetValue<T>(string key)
    {
        if (this.Values.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }
}