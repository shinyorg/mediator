using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class OfflineAvailableRequestMiddlewareTests
{
    readonly MockInternetService connectivity;
    readonly MockOfflineService offline;
    readonly OfflineAvailableRequestMiddleware<OfflineRequest, long> middleware;
    readonly OfflineRequestHandler handler;
    readonly ConfigurationManager config;
    
    public OfflineAvailableRequestMiddlewareTests(ITestOutputHelper output)
    {
        this.handler = new();
        this.connectivity = new();
        this.offline = new(TimeProvider.System, new DefaultContractKeyProvider(null));

        this.config = new ConfigurationManager();
        // this.config.AddConfiguration(new MemoryConfigurationProvider(new MemoryConfigurationSource().InitialData))
        
        this.middleware = new OfflineAvailableRequestMiddleware<OfflineRequest, long>(
            TestHelpers.CreateLogger<OfflineAvailableRequestMiddleware<OfflineRequest, long>>(output),
            this.connectivity, 
            this.offline,
            this.config
        );
    }
    
    
    [Fact]
    public async Task EndToEnd()
    {
        this.handler.ReturnValue = 99L;
        this.connectivity.IsAvailable = true;
        
        var request = new OfflineRequest();
        var context = new MockMediatorContext
        {
            Message = request,
            MessageHandler = this.handler
        };
        
        var func = () => this.middleware.Process(
            context,
            () => this.handler.Handle(request, context, CancellationToken.None),
            CancellationToken.None
        );

        var result = await func();
        result.ShouldBe(99L, "Gate 1");
        this.handler.WasHit.ShouldBe(true, "Gate 1");

        this.handler.WasHit = false;
        this.handler.ReturnValue = 88L;
        this.connectivity.IsAvailable = false;
        result = await func();
        
        this.handler.WasHit.ShouldBe(false, "Gate 2");
        result.ShouldBe(99L, "Gate 2");
    }
}


public record OfflineRequest : IRequest<long>;

public partial class OfflineRequestHandler : IRequestHandler<OfflineRequest, long>
{
    public bool WasHit { get; set; }
    public long ReturnValue { get; set; }
    
    [OfflineAvailable]
    public Task<long> Handle(OfflineRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        this.WasHit = true;
        return Task.FromResult(this.ReturnValue);
    }
}
