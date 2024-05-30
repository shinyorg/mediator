using System.Diagnostics;
using FluentAssertions;

namespace Shiny.Mediator.Tests;


public class MediatorTests
{
    [Theory]
    [InlineData(10000, 1000, false, true, false)]
    [InlineData(3000, 6000, true, false, false)]
    [InlineData(3000, 4000, true, false, true)]
    public async Task Events_TriggerTypes(int delayMs, int expectedTime, bool timeIsMinValue, bool fireAndForget, bool executeParallel)
    {
        var services = new ServiceCollection();
        services.AddShinyMediator();
        services.AddSingletonAsImplementedInterfaces<Test1EventHandler>();
        services.AddSingletonAsImplementedInterfaces<Test2EventHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var sw = new Stopwatch();
        sw.Start();
        await mediator.Publish(new TestEvent { Delay = delayMs }, fireAndForget, executeParallel);
        sw.Stop();
        
        if (timeIsMinValue)
            sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(expectedTime);
        else
            sw.ElapsedMilliseconds.Should().BeLessOrEqualTo(expectedTime);
    }

    
    [Fact]
    public async Task Missing_RequestHandler()
    {
        try
        {
            var services = new ServiceCollection();
            services.AddShinyMediator();
            var sp = services.BuildServiceProvider();
            var mediator = sp.GetRequiredService<IMediator>();
            await mediator.Send(new TestRequest());
            Assert.Fail("This should not have passed");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Be("No request handler found for Shiny.Mediator.Tests.TestRequest");
        }
    }
    
    [Fact]
    public async Task Registration_OnlyOneRequestHandler_NoResponse()
    {
        try
        {
            var services = new ServiceCollection();
            services.AddShinyMediator();
            services.AddSingletonAsImplementedInterfaces<Test1RequestHandler>();
            services.AddSingletonAsImplementedInterfaces<Test2RequestHandler>();
            var sp = services.BuildServiceProvider();
            var mediator = sp.GetRequiredService<IMediator>();
            await mediator.Send(new TestRequest());
            Assert.Fail("This should not have passed");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Be("More than 1 request handlers found for Shiny.Mediator.Tests.TestRequest");
        }
    }
    
    // [Fact]
    // public void Registration_OnlyOneRequestHandler_WithResponse()
    // {
    //     
    // }


    [Fact]
    public async Task Events_SubscriptionFired()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var tcs = new TaskCompletionSource();
        mediator.Subscribe<TestEvent>((@event, ct) =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        await mediator.Publish(new TestEvent());
        tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
    }
}


public class TestRequest : IRequest
{
    public int Delay { get; set; }
}

public class TestEvent : IEvent
{
    public int Delay { get; set; }
}


public class Test1RequestHandler : IRequestHandler<TestRequest>
{
    public async Task Handle(TestRequest request, CancellationToken cancellationToken)
    {
        if (request.Delay > 0)
            await Task.Delay(request.Delay);
    }
}


public class Test2RequestHandler : IRequestHandler<TestRequest>
{
    public async Task Handle(TestRequest request, CancellationToken cancellationToken)
    {
    }
}

public class Test1EventHandler : IEventHandler<TestEvent>
{
    public async Task Handle(TestEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Delay > 0)
            await Task.Delay(@event.Delay);
    }
}
public class Test2EventHandler : IEventHandler<TestEvent>
{
    public async Task Handle(TestEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Delay > 0)
            await Task.Delay(@event.Delay);
    }
}