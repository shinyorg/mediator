using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Tests;


public class OfflineAvailableRequestMiddlewareTests
{
    readonly MockInternetService connectivity;
    readonly MockStorageService storeMgr;
    readonly OfflineAvailableRequestMiddleware<OfflineRequest, long> middleware;
    readonly OfflineRequestHandler handler;
    readonly ConfigurationManager config;
    
    public OfflineAvailableRequestMiddlewareTests()
    {
        this.handler = new();
        this.connectivity = new();
        this.storeMgr = new();

        this.config = new ConfigurationManager();
        // this.config.AddConfiguration(new MemoryConfigurationProvider(new MemoryConfigurationSource().InitialData))
        
        this.middleware = new OfflineAvailableRequestMiddleware<OfflineRequest, long>(
            null,
            this.connectivity, 
            this.storeMgr,
            null
        );
    }
    
    
    [Fact]
    public async Task EndToEnd()
    {
        this.handler.ReturnValue = 99L;
        this.connectivity.IsAvailable = true;
        
        var func = () => this.middleware.Process(
            new OfflineRequest(),
            () => this.handler.Handle(new OfflineRequest(), CancellationToken.None),
            this.handler,
            CancellationToken.None
        );

        var result = await func();
        result.Should().Be(99L, "Gate 1");
        this.handler.WasHit.Should().Be(true, "Gate 1");

        this.handler.WasHit = false;
        this.handler.ReturnValue = 88L;
        this.connectivity.IsAvailable = false;
        result = await func();
        
        this.handler.WasHit.Should().Be(false, "Gate 2");
        result.Should().Be(99L, "Gate 2");
    }
}


public record OfflineRequest : IRequest<long>;

public class OfflineRequestHandler : IRequestHandler<OfflineRequest, long>
{
    public bool WasHit { get; set; }
    public long ReturnValue { get; set; }
    
    [OfflineAvailable]
    public Task<long> Handle(OfflineRequest request, CancellationToken cancellationToken)
    {
        this.WasHit = true;
        return Task.FromResult(this.ReturnValue);
    }
}

public class MockStorageService : IStorageService
{
    public Task Store(object request, object result)
    {
        throw new NotImplementedException();
    }

    public Task<TResult?> Get<TResult>(object request)
    {
        throw new NotImplementedException();
    }

    public Task Clear() => Task.CompletedTask;
}

public class MockInternetService : IInternetService
{
    public bool IsAvailable { get; set; }
    public Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}