using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Mediator;

namespace Sample.Handlers;


public record ErrorCommand : ICommand;

[SingletonHandler]
public class ErrorCommandHandler : ICommandHandler<ErrorCommand>
{
    public Task Handle(ErrorCommand command, CommandContext<ErrorCommand> context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Why you call me?");
    }
}