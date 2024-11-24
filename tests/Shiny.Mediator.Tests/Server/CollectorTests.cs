using Shiny.Mediator.Server.Client.Infrastructure;

namespace Shiny.Mediator.Tests.Server;


public class CollectorTests
{
    [Fact]
    public void Detected()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(IEventHandler<>), typeof(CollectorEventHandler));
        services.AddSingleton(typeof(IRequestHandler<>), typeof(CollectorTestRequestHandler));
        services.AddSingleton(typeof(IRequestHandler<,>), typeof(CollectorTestResultRequestHandler));
        
        var collector = new ContractCollector(services);
        var types = collector.Collect().ToList();
        
        types.Should().Contain(typeof(CollectorTestEvent));
        types.Should().Contain(typeof(CollectorTestRequest));
        types.Should().Contain(typeof(CollectorTestResultRequest));
        types.Should().NotContain(typeof(TestEvent));
    }
}


public class CollectorTestEvent : IEvent {}
public class CollectorTestRequest : IRequest {}
public class CollectorTestResultRequest : IRequest<string> {}

public class CollectorEventHandler : IEventHandler<CollectorTestEvent>
{
    public Task Handle(CollectorTestEvent @event, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class CollectorTestRequestHandler : IRequestHandler<CollectorTestRequest>
{
    public Task Handle(CollectorTestRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
public class CollectorTestResultRequestHandler : IRequestHandler<CollectorTestResultRequest, string>
{
    public Task<string> Handle(CollectorTestResultRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(String.Empty);
    }
}