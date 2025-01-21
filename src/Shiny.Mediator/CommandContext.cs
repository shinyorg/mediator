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

public class CommandContext<TCommand>(ICommandHandler<TCommand> handler, TCommand command) : CommandContext(handler, command)
    where TCommand : ICommand
{
    public new TCommand Command => command;
    public new ICommandHandler<TCommand> Handler => handler;
}