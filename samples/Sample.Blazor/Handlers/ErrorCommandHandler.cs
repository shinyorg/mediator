using Shiny.Mediator;

namespace Sample.Blazor.Handlers;

public record ErrorCommand(bool HandleIt) : ICommand;


[SingletonHandler]
public class ErrorCommandHandler : ICommandHandler<ErrorCommand>
{
    public Task Handle(ErrorCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler BOOOM!");
    }
}