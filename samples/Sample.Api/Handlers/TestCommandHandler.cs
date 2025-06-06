namespace Sample.Api.Handlers;

public record TestCommand(int Number, string StringArg) : ICommand;


[MediatorHttpGroup("/test")]
public class TestCommandHandler : ICommandHandler<TestCommand>
{
    [MediatorHttpPost("TestCommand", "/command")]
    public Task Handle(TestCommand request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}