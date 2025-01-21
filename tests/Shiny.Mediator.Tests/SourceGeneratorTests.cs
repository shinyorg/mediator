namespace Shiny.Mediator.Tests;

public class SourceGeneratorTests
{
    [Fact]
    public void DidRegister()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator();
        services.AddDiscoveredMediatorHandlersFromUnitTests(); // assembly name changed to get around assembly name (Shiny.Mediator) detection
        var sp = services.BuildServiceProvider();

        sp.GetService<IEventHandler<SourceGenEvent>>().ShouldNotBeNull("Event Handler not found");
        sp.GetService<ICommandHandler<SourceGenCommand>>().ShouldNotBeNull("Command Handler not found");
        sp.GetService<IRequestHandler<SourceGenResponseRequest, SourceGenResponse>>().ShouldNotBeNull("Request/Response Handler not found");
    }
}

public record SourceGenCommand : ICommand;
public record SourceGenResponseRequest : IRequest<SourceGenResponse>;
public record SourceGenResponse;
public record SourceGenEvent : IEvent;


[SingletonHandler]
public class SourceGenCommandHandler : ICommandHandler<SourceGenCommand>
{
    public Task Handle(SourceGenCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}
[SingletonHandler]
public class SourceGenResponseRequestHandler : IRequestHandler<SourceGenResponseRequest, SourceGenResponse>
{
    public Task<SourceGenResponse> Handle(SourceGenResponseRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SourceGenResponse());
}
[SingletonHandler]
public class SourceGenEventHandler : IEventHandler<SourceGenEvent>
{
    public Task Handle(SourceGenEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}