using Shiny.Mediator;
using ICommand = Shiny.Mediator.ICommand;

namespace Sample.Handlers;


public record ErrorCommand : ICommand;

[SingletonHandler]
public class ErrorCommandHandler : ICommandHandler<ErrorCommand>
{
    public Task Handle(ErrorCommand command, MediatorContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Why you call me?");
    }
}