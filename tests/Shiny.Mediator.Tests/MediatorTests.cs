using Shiny.Mediator.Impl;

namespace Shiny.Mediator.Tests;


public class MediatorTests
{
    [Fact]
    public async Task Events_FireAndForget()
    {
        var services = new ServiceCollection();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        
    }
    
    
    [Fact]
    public async Task Events_ParallelExecution()
    {
        
    }    

    
    [Fact]
    public void Registration_OnlyOneRequestHandler_NoResponse()
    {
        
    }
    
    [Fact]
    public void Registration_OnlyOneRequestHandler_WithResponse()
    {
        
    }


    [Fact]
    public void Events_SubscriptionFired()
    {
        var services = new ServiceCollection();
    }
}