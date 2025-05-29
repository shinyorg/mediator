using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class HttpDirectRequestHandlerTests(ITestOutputHelper output)
{
    [Fact(Skip = "TODO")]
    public async Task e2e()
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        services.AddConfiguration(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "", "" }
            });
        });
        services.AddShinyMediator(cfg =>
        {
            cfg.AddHttpClient();
        }, false);
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var request = new HttpDirectRequest
        {
            ConfigNameOrRoute = "Test",
            ResultType = typeof(object)
        };
        // mediator.Request(request, TestContext.Current);
    }
}