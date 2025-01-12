using Sample.Contracts;

namespace Sample.Handlers;


[SingletonHandler]
public class MyValidateCommandHandler : ICommandHandler<MyValidateCommand>
{
    public Task Handle(MyValidateCommand command, CancellationToken cancellationToken)
        => Task.CompletedTask;
}