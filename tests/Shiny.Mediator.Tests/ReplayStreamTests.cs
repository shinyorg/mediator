
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class ReplayStreamTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ContextUpdatingBetweenAwaits()
    {
        var services = new ServiceCollection();
        services.AddLogging(x => x.AddXUnit(output));
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddShinyMediator();

        services.AddSingletonAsImplementedInterfaces<MockOfflineService>();
        services.AddSingletonAsImplementedInterfaces<MockInternetService>();
        services.AddSingletonAsImplementedInterfaces<ReplayStreamRequestHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var internet = sp.GetRequiredService<MockInternetService>();
        internet.IsAvailable = false;
        
        var context = mediator.RequestWithContext(new ReplayStreamRequest());

        var i = 0;
        await foreach (var item in context.Result)
        {
            var cache = context.Context.Cache();
            cache.ShouldBeNull("Cache should be null");
            context.Context.Headers.ContainsKey("FromHandler").ShouldBeTrue();
            
            var offline = context.Context.Offline();
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
    public async IAsyncEnumerable<string> Handle(ReplayStreamRequest request, MediatorContext context, CancellationToken cancellationToken)
    {
        context.Add("FromHandler", true);
        yield return "Hello";
    }
}