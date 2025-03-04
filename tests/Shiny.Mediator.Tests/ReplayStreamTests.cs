using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class ReplayStreamTests(ITestOutputHelper output)
{
    [Fact(Skip = "Borked")]
    public async Task ContextUpdatingBetweenAwaits()
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        services.AddConfiguration();
        services.AddShinyMediator();

        services.AddSingletonAsImplementedInterfaces<MockOfflineService>();
        services.AddSingletonAsImplementedInterfaces<MockInternetService>();
        services.AddSingletonAsImplementedInterfaces<ReplayStreamRequestHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var internet = sp.GetRequiredService<MockInternetService>();
        internet.IsAvailable = false;

        IMediatorContext context = null!;
        var enumerable = mediator.Request(new ReplayStreamRequest(), CancellationToken.None, ctx => context = ctx);

        var i = 0;
        await foreach (var item in enumerable)
        {
            var cache = context.Cache();
            cache.ShouldBeNull("Cache should be null");
            context.Headers.ContainsKey("FromHandler").ShouldBeTrue();
            
            var offline = context.Offline();
            switch (i)
            {
                case 0:
                    offline.ShouldBeNull("Offline should be null");
                    internet.IsAvailable = true;
                    break;
                
                case 1:
                    offline.ShouldNotBeNull("Offline should be null");
                    break;
            }
            i++;
        }
    }
}

public class ReplayStreamRequest : IStreamRequest<string>;

public class ReplayStreamRequestHandler : IStreamRequestHandler<ReplayStreamRequest, string>
{
    [ReplayStream]
    public async IAsyncEnumerable<string> Handle(ReplayStreamRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        context.AddHeader("FromHandler", true);
        yield return "Hello";
    }
}