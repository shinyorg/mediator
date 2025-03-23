using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Caching.Infrastructure;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.Caching;


public class CachingRequestMiddlewareTests
{
    readonly ILogger<CachingRequestMiddleware<CachingRequestMiddlewareRequest, string>> logger;
    readonly FakeTimeProvider timeProvider;
    readonly ConfigurationManager config;
    readonly MockCacheService cache;
    readonly CachingRequestMiddleware<CachingRequestMiddlewareRequest, string> middleware;
    
    
    public CachingRequestMiddlewareTests(ITestOutputHelper output)
    {
        this.timeProvider = new FakeTimeProvider();
        this.logger = TestHelpers.CreateLogger<CachingRequestMiddleware<CachingRequestMiddlewareRequest, string>>(output);
        this.config = new ConfigurationManager();
        this.cache = new MockCacheService(this.timeProvider);
        
        this.middleware = new CachingRequestMiddleware<CachingRequestMiddlewareRequest, string>(logger, config, cache);
    }
    
    
    [Fact]
    public void Test()
    {

    }
}

public record CachingRequestMiddlewareRequest(string Value) : IRequest<string>;