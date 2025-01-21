using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Tests;


public class OfflineAvailableRequestMiddlewareTests
{
    readonly MockInternetService connectivity;
    readonly MockOfflineService offline;
    readonly OfflineAvailableRequestMiddleware<OfflineRequest, long> middleware;
    readonly OfflineRequestHandler handler;
    readonly ConfigurationManager config;
    
    public OfflineAvailableRequestMiddlewareTests()
    {
        this.handler = new();
        this.connectivity = new();
        this.offline = new();

        this.config = new ConfigurationManager();
        // this.config.AddConfiguration(new MemoryConfigurationProvider(new MemoryConfigurationSource().InitialData))
        
        this.middleware = new OfflineAvailableRequestMiddleware<OfflineRequest, long>(
            null,
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
        var context = new RequestContext<OfflineRequest>(request, this.handler);
        
        var func = () => this.middleware.Process(
            context,
            () => this.handler.Handle(context.Request, context, CancellationToken.None),
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

public class OfflineRequestHandler : IRequestHandler<OfflineRequest, long>
{
    public bool WasHit { get; set; }
    public long ReturnValue { get; set; }
    
    [OfflineAvailable]
    public Task<long> Handle(OfflineRequest request, RequestContext<OfflineRequest> context, CancellationToken cancellationToken)
    {
        this.WasHit = true;
        return Task.FromResult(this.ReturnValue);
    }
}

public class MockOfflineService : IOfflineService
{
    public Task<string> Set(object request, object result)
    {
        throw new NotImplementedException();
    }

    public Task<OfflineResult<TResult>?> Get<TResult>(object request)
    {
        throw new NotImplementedException();
    }

    public Task ClearByType(Type requestType)
    {
        throw new NotImplementedException();
    }

    public Task ClearByRequest(object request)
    {
        throw new NotImplementedException();
    }

    public Task Clear()
    {
        throw new NotImplementedException();
    }
}

public class MockInternetService : IInternetService
{
    public bool IsAvailable { get; set; }
    public Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}