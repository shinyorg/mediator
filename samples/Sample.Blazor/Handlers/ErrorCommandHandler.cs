using Shiny.Mediator;

namespace Sample.Blazor.Handlers;

public record ErrorCommand : ICommand;

public record SafeErrorCommand : ICommand;


[SingletonHandler]
public class ErrorCommandHandler : ICommandHandler<ErrorCommand>, ICommandHandler<SafeErrorCommand>
{
    public Task Handle(ErrorCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler BOOOM!");
    }

    public Task Handle(SafeErrorCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("I also blow up, but you should see an alert instead of Blazor crashing");
    }
}