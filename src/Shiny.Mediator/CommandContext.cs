namespace Shiny.Mediator;

public class CommandContext(ICommandHandler handler, ICommand command)
{
    readonly Dictionary<string, object> store = new();
    
    public Guid CommandId { get; }= Guid.NewGuid();
    public IReadOnlyDictionary<string, object> Values => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);

    public ICommand Command => command;
    public ICommandHandler Handler => handler;
}