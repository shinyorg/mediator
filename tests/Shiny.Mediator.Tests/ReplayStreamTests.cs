
namespace Shiny.Mediator.Tests;

public class ReplayStreamTests
{
    [Fact]
    public async Task ContextUpdatingBetweenAwaits()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator();
        // TODO: add cache & offline
        services.AddSingletonAsImplementedInterfaces<ReplayStreamRequestHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        
        var context = mediator.RequestWithContext(new ReplayStreamRequest());

        await foreach (var item in context.Result)
        {
            // TODO: these should clear after the "first" pump?
            var cache = context.Context.Cache();

            var offline = context.Context.Offline();
            
            // context.Context.Values.ContainsKey("FromHandler")
        }
    }
}

public class ReplayStreamRequest : IStreamRequest<string>;

public class ReplayStreamRequestHandler : IStreamRequestHandler<ReplayStreamRequest, string>
{
    [ReplayStream]
    public async IAsyncEnumerable<string> Handle(ReplayStreamRequest request, RequestContext<ReplayStreamRequest> context, CancellationToken cancellationToken)
    {
        context.Add("FromHandler", true);
        yield return "Hello";
    }
}