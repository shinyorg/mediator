namespace Shiny.Mediator.Tests;


public class RequestHandlerTests
{
    [Fact]
    public async Task EndToEnd()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddShinyMediator(includeStandardMiddleware: false);
        services.AddSingletonAsImplementedInterfaces<TestResultRequestHandler>();
        var sp = services.BuildServiceProvider();
        var response = await sp.GetRequiredService<IMediator>().Request(new TestResultRequest("HELLO"));
        response.Result.ShouldBe("RESPONSE-HELLO");
    }


    [Fact]
    public async Task EndToEndWithContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddShinyMediator(includeStandardMiddleware: false);
        services.AddSingletonAsImplementedInterfaces<TestResultRequestHandler>();
        var sp = services.BuildServiceProvider();
        var result = await sp
            .GetRequiredService<IMediator>()
            .Request(
                new TestResultRequest("HELLO"), 
                CancellationToken.None,
                ctx => ctx.AddHeader("Hello", "World")
            );
        
        result.Result.ShouldBe("RESPONSE-HELLO");
        result.Context.Headers.ShouldContainKey("Hello");
    }
}


public record TestResultRequest(string Arg) : IRequest<string>;

public class TestResultRequestHandler : IRequestHandler<TestResultRequest, string>
{
    public Task<string> Handle(TestResultRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult("RESPONSE-" + request.Arg.ToUpper());
}