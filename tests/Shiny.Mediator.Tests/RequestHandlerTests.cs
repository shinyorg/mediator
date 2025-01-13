namespace Shiny.Mediator.Tests;


public class RequestHandlerTests
{
    [Fact]
    public async Task EndToEnd()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddShinyMediator();
        services.AddSingletonAsImplementedInterfaces<TestResultRequestHandler>();
        var sp = services.BuildServiceProvider();
        var result = await sp.GetRequiredService<IMediator>().Request(new TestResultRequest("HELLO"));
        result.Should().Be("RESPONSE-HELLO");
    }
}


public record TestResultRequest(string Arg) : IRequest<string>;

public class TestResultRequestHandler : IRequestHandler<TestResultRequest, string>
{
    public Task<string> Handle(TestResultRequest request, CancellationToken cancellationToken)
        => Task.FromResult("RESPONSE-" + request.Arg);
}