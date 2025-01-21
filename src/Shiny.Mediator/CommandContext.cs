namespace Shiny.Mediator;

public class CommandContext(ICommandHandler handler, ICommand command) : AbstractMediatorContext
{
    public ICommand Command => command;
    public ICommandHandler Handler => handler;
}

public class CommandContext<TCommand>(ICommandHandler<TCommand> handler, TCommand command) : CommandContext(handler, command)
    where TCommand : ICommand
{
    public new TCommand Command => command;
    public new ICommandHandler<TCommand> Handler => handler;
}