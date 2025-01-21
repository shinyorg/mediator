namespace Shiny.Mediator;

public interface IMediatorContext
{
    Guid Id { get; }
    public IReadOnlyDictionary<string, object> Values { get; }
    public void Add(string key, object value);
}

public abstract class AbstractMediatorContext : IMediatorContext
{
    public Guid Id { get; protected set; }

    readonly Dictionary<string, object> store = new();
    public IReadOnlyDictionary<string, object> Values => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);
}