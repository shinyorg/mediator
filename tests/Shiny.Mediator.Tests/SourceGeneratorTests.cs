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

        sp.GetService<IEventHandler<SourceGenEvent>>().Should().NotBeNull("Event Handler not found");
        sp.GetService<IRequestHandler<SourceGenRequest>>().Should().NotBeNull("Request Handler not found");
        sp.GetService<IRequestHandler<SourceGenResponseRequest, SourceGenResponse>>().Should().NotBeNull("Request/Response Handler not found");
    }
}

public record SourceGenRequest : IRequest;
public record SourceGenResponseRequest : IRequest<SourceGenResponse>;
public record SourceGenResponse;
public record SourceGenEvent : IEvent;


[RegisterHandler]
public class SourceGenRequestHandler : IRequestHandler<SourceGenRequest>
{
    public Task Handle(SourceGenRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}
[RegisterHandler]
public class SourceGenResponseRequestHandler : IRequestHandler<SourceGenResponseRequest, SourceGenResponse>
{
    public Task<SourceGenResponse> Handle(SourceGenResponseRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new SourceGenResponse());
}
[RegisterHandler]
public class SourceGenEventHandler : IEventHandler<SourceGenEvent>
{
    public Task Handle(SourceGenEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}