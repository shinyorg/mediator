namespace Sample.Api.Handlers;

public record TestCommand(int Number, string StringArg) : ICommand;


[ScopedHandler]
[MediatorHttpPost("TestCommand", "/testcommand")]
public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public Task Handle(TestCommand request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}