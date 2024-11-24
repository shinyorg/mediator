using Shiny.Mediator.Server.Client;

namespace Shiny.Mediator.Tests.Server;

public class MediaHubOptionsTests
{
    [Fact]
    public void CollectedTypeHaveUri()
    {
        
        var options = new MediatorHubOptions();
        options.Map<CollectorTestEvent>(new Uri("http://type-specific"));
        options.Map("Shiny.Mediator.Tests.Server", new Uri("http://ns-specific"));
        options.Map("Shiny.Mediator.Tests.*", new Uri("http://ns-alltests"));
        options.Map(typeof(CollectorTestEvent).Assembly, new Uri("http://assembly"));
        
        
    }
}