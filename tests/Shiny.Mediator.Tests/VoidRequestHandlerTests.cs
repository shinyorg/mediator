namespace Shiny.Mediator.Tests;


public class VoidRequestHandlerTests
{
    [Fact]
    public async Task Missing_RequestHandler()
    {
        try
        {
            var services = new ServiceCollection();
            services.AddShinyMediator(cfg => { });
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
}


public class TestRequest : IRequest
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