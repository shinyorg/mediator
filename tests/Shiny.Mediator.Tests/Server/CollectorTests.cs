using Shiny.Mediator.Server.Client.Infrastructure;

namespace Shiny.Mediator.Tests.Server;


public class CollectorTests
{
    [Fact]
    public void Detected()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(IEventHandler<>), typeof(CollectorEventHandler));
        services.AddSingleton(typeof(IRequestHandler<,>), typeof(CollectorTestRequestHandler));
        
        var collector = new ContractCollector(services);
        var types = collector.Collect().ToList();
        
        types.Should().Contain(typeof(CollectorTestEvent));
        types.Should().Contain(typeof(CollectorTestRequest));
        types.Should().NotContain(typeof(TestEvent));
    }
}