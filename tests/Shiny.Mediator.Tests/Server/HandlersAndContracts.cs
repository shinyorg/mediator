using Shiny.Mediator.Server;

namespace Shiny.Mediator.Tests.Server;

public class CollectorTestEvent : IServerEvent {}
public class CollectorTestRequest : IServerRequest<string> {}

public class CollectorEventHandler : IEventHandler<CollectorTestEvent>
{
    public Task Handle(CollectorTestEvent @event, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class CollectorTestRequestHandler : IRequestHandler<CollectorTestRequest, string>
{
    public Task<string> Handle(CollectorTestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(String.Empty);
    }
}