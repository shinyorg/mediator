using Sample.Contracts;

namespace Sample.Handlers;


[SingletonHandler]
public class MyValidateCommandHandler : ICommandHandler<MyValidateCommand>
{
    public Task Handle(MyValidateCommand command, CommandContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}