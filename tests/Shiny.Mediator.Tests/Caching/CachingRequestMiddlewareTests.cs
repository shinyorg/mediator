using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Caching;
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
    public async Task EndToEnd_NotFromCache()
    {
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("NotFromCache"));
        var context = new MockMediatorContext
        {
            Message = new CachingRequestMiddlewareRequest("Hello"),
            MessageHandler = new CacheRequestMiddlewareTestHandler()
        };
        var result = await this.middleware.Process(context, handler, CancellationToken.None);
        
        result.ShouldBe("NotFromCache");
    }
    
    
    [Fact]
    public async Task EndToEnd_FromCache()
    {
        await this.cache.Set("Test", "FromCache");
        
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("NotFromCache"));
        var context = new MockMediatorContext
        {
            Message = new CachingRequestMiddlewareRequest("Hello"),
            MessageHandler = new CacheRequestMiddlewareTestHandler()
        };
        var result = await this.middleware.Process(context, handler, CancellationToken.None);
        
        result.ShouldBe("FromCache");
    }
}

public record CachingRequestMiddlewareRequest(string Value) : IRequest<string>, ICacheControl, IRequestKey
{
    public bool ForceRefresh { get; set; }
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }

    public string GetKey() => "Test";
};

public class CacheRequestMiddlewareTestHandler : IRequestHandler<CachingRequestMiddlewareRequest, string>
{
    public Task<string> Handle(CachingRequestMiddlewareRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult("FromHandler");
    }
}