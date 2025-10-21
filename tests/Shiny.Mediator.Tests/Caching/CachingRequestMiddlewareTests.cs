using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Caching.Infrastructure;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.Caching;


public class CachingRequestMiddlewareTests(ITestOutputHelper output)
{
    readonly FakeTimeProvider timeProvider = new();
    readonly ConfigurationManager config = new();

    
    [Fact]
    public async Task DirectMiddleware_NotFromCache()
    {
        var app = this.MiddlewareSetup();
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("NotFromCache"));
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);
        
        result.ShouldBe("NotFromCache");
    }
    
    
    [Fact]
    public async Task DirectMiddleware_FromCache()
    {
        var app = this.MiddlewareSetup();
        await app.Cache.Set("Test", "FromCache");
        
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("NotFromCache"));
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);
        
        result.ShouldBe("NotFromCache");
    }

    
    [Fact]
    public async Task MediatorContext_CacheConfig()
    {
        var app = this.MiddlewareSetup();
        await app.Cache.Set("Test", "FromCache");
        
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("NotFromCache"));
        app.Context.SetCacheConfig(new CacheItemConfig
        {
            AbsoluteExpiration = TimeSpan.FromSeconds(33)
        });
        
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);
        result.ShouldBe("FromCache");

        var cache = app.Context.Cache();
        cache?.Config?.AbsoluteExpiration.ShouldNotBeNull();
        cache!.Config!.AbsoluteExpiration.ShouldBe(TimeSpan.FromSeconds(33));
    }
    
    
    [Fact]
    public async Task MediatorContext_ForceRefresh()
    {
        var app = this.MiddlewareSetup();
        app.Context.SetCacheConfig(new CacheItemConfig
        {
            AbsoluteExpiration = TimeSpan.FromSeconds(18)
        });
        await app.Cache.Set("Test", "FromCache");
        
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("NotFromCache"));
        app.Context.ForceCacheRefresh();
        
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);
        result.ShouldBe("NotFromCache");

        var cache = app.Context.Cache();
        cache.ShouldNotBeNull();
        cache.IsHit.ShouldBeFalse();
    }
    

    (
        IMediatorContext Context,
        MockCacheService Cache, 
        ILogger<CachingRequestMiddleware<CachingContractMiddlewareContract, string>> Logger, 
        CachingRequestMiddleware<CachingContractMiddlewareContract, string> Middleware
    ) MiddlewareSetup(string requestArg = "NotFromCache")
    {
        var context = new MockMediatorContext
        {
            Message = new CachingContractMiddlewareContract(requestArg),
            MessageHandler = new CachingRequestMiddlewareRequestHandler()
        };
        var cache = new MockCacheService(timeProvider);
        var contractKeyProvider = new DefaultContractKeyProvider(null);
        var logger = TestHelpers.CreateLogger<CachingRequestMiddleware<CachingContractMiddlewareContract, string>>(output);
        var middleware = new CachingRequestMiddleware<CachingContractMiddlewareContract, string>(logger, this.config, cache, contractKeyProvider);
        
        return (context, cache, logger, middleware);
    }
    

    [Fact]
    public async Task Mediator_ConfigFromAttribute()
    {
        var app = this.StandardCacheMediator(x => x.AddSingletonAsImplementedInterfaces<AttributedTestHandler>());

        var result = await app.Mediator.Request(new AttributedTestContract("Test1"));
        result.Result.ShouldBe("Test1");
            
        var cache = result.Context.Cache();
        cache.ShouldNotBeNull();
        cache.IsHit.ShouldBeFalse("Cache should not be hit");
        
        result = await app.Mediator.Request(new AttributedTestContract("Test2"));
        result.Result.ShouldBe("Test1"); // still test one
        
        cache = result.Context.Cache();
        cache.ShouldNotBeNull("Cache should not be null");
        cache.IsHit.ShouldBeTrue("Cache should be hit");
        cache.Config.ShouldNotBeNull("Cache config should not be null");
        cache.Config.SlidingExpiration!.Value.TotalSeconds.ShouldBe(99);
    }
    
    
    [Fact]
    public async Task MediatorContext_ForceCacheRefresh()
    {
        var app = this.StandardCacheMediator(x => x.AddSingletonAsImplementedInterfaces<AttributedTestHandler>());

        var result = await app.Mediator.Request(new AttributedTestContract("Test1"));
        result.Result.ShouldBe("Test1");
            
        var cache = result.Context.Cache();
        cache.ShouldNotBeNull();
        cache.IsHit.ShouldBeFalse("Cache should not be hit");
        
        result = await app.Mediator.Request(new AttributedTestContract("Test2"), CancellationToken.None, ctx => ctx.ForceCacheRefresh());
        result.Result.ShouldBe("Test2"); // still test one
        
        cache = result.Context.Cache();
        cache.ShouldNotBeNull("Cache should not be null");
        cache.IsHit.ShouldBeFalse("Cache should be hit");
    }
    

    (IServiceProvider Services, IMediator Mediator) StandardCacheMediator(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(this.config);
        services.AddSingleton<TimeProvider>(this.timeProvider);
        services.AddLogging(x => x.AddXUnit(output));
        services.AddShinyMediator(x => x.AddCaching<MockCacheService>(), false);
        configure.Invoke(services);

        var sp = services.BuildServiceProvider();
        
        return (sp, sp.GetRequiredService<IMediator>());
    }
}

public record CachingContractMiddlewareContract(string Value) : IRequest<string>, IContractKey
{
    public string GetKey() => "Test";
};

public class CachingRequestMiddlewareRequestHandler : IRequestHandler<CachingContractMiddlewareContract, string>
{
    public Task<string> Handle(CachingContractMiddlewareContract contract, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult("FromHandler");
    }
}

public record AttributedTestContract(string Value) : IRequest<string>, IContractKey
{
    public string GetKey() => "Test";
};
public partial class AttributedTestHandler : IRequestHandler<AttributedTestContract, string>
{
    [Cache(SlidingExpirationSeconds = 99)]
    public Task<string> Handle(AttributedTestContract contract, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(contract.Value);
    }
}