using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class HttpRequestCacheMiddlewareTests(ITestOutputHelper output)
{
    readonly FakeTimeProvider timeProvider = new();


    [Fact]
    public async Task CacheMiss_CallsNextAndCachesResult()
    {
        // Arrange
        var app = this.MiddlewareSetup();
        var handlerCalled = false;
        var handler = new RequestHandlerDelegate<string>(() =>
        {
            handlerCalled = true;
            return Task.FromResult("HandlerResult");
        });

        // Set up HTTP response with Cache-Control header
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Headers.CacheControl = new CacheControlHeaderValue
        {
            MaxAge = TimeSpan.FromMinutes(5)
        };
        app.Context.SetHttp(new HttpRequestMessage(), httpResponse);

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBe("HandlerResult");
        handlerCalled.ShouldBeTrue("Handler should be called on cache miss");
        app.Cache.Items.ShouldContainKey("TestKey");
    }


    [Fact]
    public async Task CacheHit_ReturnsCachedValue_DoesNotCallHandler()
    {
        // Arrange
        var app = this.MiddlewareSetup();

        // Pre-populate cache - store the CacheEntry directly (as the middleware does)
        var cacheEntry = new CacheEntry<string>("TestKey", "CachedValue", timeProvider.GetUtcNow());
        app.Cache.Items["TestKey"] = cacheEntry;

        var handlerCalled = false;
        var handler = new RequestHandlerDelegate<string>(() =>
        {
            handlerCalled = true;
            return Task.FromResult("HandlerResult");
        });

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBe("CachedValue");
        handlerCalled.ShouldBeFalse("Handler should NOT be called on cache hit");
    }


    [Fact]
    public async Task ForceCacheRefresh_CallsHandlerEvenWhenCached()
    {
        // Arrange
        var app = this.MiddlewareSetup();

        // Pre-populate cache - store the CacheEntry directly (as the middleware does)
        var cacheEntry = new CacheEntry<string>("TestKey", "CachedValue", timeProvider.GetUtcNow());
        app.Cache.Items["TestKey"] = cacheEntry;

        // Force cache refresh
        app.Context.ForceCacheRefresh();

        var handlerCalled = false;
        var handler = new RequestHandlerDelegate<string>(() =>
        {
            handlerCalled = true;
            return Task.FromResult("FreshValue");
        });

        // Set up HTTP response with Cache-Control header to allow caching the new value
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Headers.CacheControl = new CacheControlHeaderValue
        {
            MaxAge = TimeSpan.FromMinutes(5)
        };
        app.Context.SetHttp(new HttpRequestMessage(), httpResponse);

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBe("FreshValue");
        handlerCalled.ShouldBeTrue("Handler should be called when forcing cache refresh");
    }


    [Fact]
    public async Task NoCache_DoesNotCacheResult()
    {
        // Arrange
        var app = this.MiddlewareSetup();
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("HandlerResult"));

        // Set up HTTP response with no-cache directive
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
        app.Context.SetHttp(new HttpRequestMessage(), httpResponse);

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBe("HandlerResult");
        app.Cache.Items.ShouldNotContainKey("TestKey");
    }


    [Fact]
    public async Task MaxAgeZero_DoesNotCacheResult()
    {
        // Arrange
        var app = this.MiddlewareSetup();
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("HandlerResult"));

        // Set up HTTP response with zero max-age
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Headers.CacheControl = new CacheControlHeaderValue
        {
            MaxAge = TimeSpan.Zero
        };
        app.Context.SetHttp(new HttpRequestMessage(), httpResponse);

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBe("HandlerResult");
        app.Cache.Items.ShouldNotContainKey("TestKey");
    }


    [Fact]
    public async Task NoCacheControlHeader_DoesNotCacheResult()
    {
        // Arrange
        var app = this.MiddlewareSetup();
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("HandlerResult"));

        // Set up HTTP response without Cache-Control header
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        app.Context.SetHttp(new HttpRequestMessage(), httpResponse);

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBe("HandlerResult");
        app.Cache.Items.ShouldNotContainKey("TestKey");
    }


    [Fact]
    public async Task NoHttpResponse_DoesNotCacheResult()
    {
        // Arrange
        var app = this.MiddlewareSetup();
        var handler = new RequestHandlerDelegate<string>(() => Task.FromResult("HandlerResult"));

        // No HTTP response set in context

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBe("HandlerResult");
        app.Cache.Items.ShouldNotContainKey("TestKey");
    }


    [Fact]
    public async Task ForceCacheRefresh_NullResult_DoesNotAttemptCache()
    {
        // Arrange
        var app = this.MiddlewareSetupNullable();
        app.Context.ForceCacheRefresh();

        var handler = new RequestHandlerDelegate<string?>(() => Task.FromResult<string?>(null));

        // Act
        var result = await app.Middleware.Process(app.Context, handler, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        app.Cache.Items.ShouldNotContainKey("TestKey");
    }


    (
        MockMediatorContext Context,
        MockCacheService Cache,
        ILogger<HttpRequestCacheMiddleware<HttpCacheTestContract, string>> Logger,
        HttpRequestCacheMiddleware<HttpCacheTestContract, string> Middleware
    ) MiddlewareSetup()
    {
        var context = new MockMediatorContext
        {
            Message = new HttpCacheTestContract("TestValue")
        };
        var cache = new MockCacheService(timeProvider);
        var contractKeyProvider = new DefaultContractKeyProvider(null!);
        var logger = TestHelpers.CreateLogger<HttpRequestCacheMiddleware<HttpCacheTestContract, string>>(output);
        var middleware = new HttpRequestCacheMiddleware<HttpCacheTestContract, string>(
            logger,
            timeProvider,
            cache,
            contractKeyProvider
        );

        return (context, cache, logger, middleware);
    }


    (
        MockMediatorContext Context,
        MockCacheService Cache,
        ILogger<HttpRequestCacheMiddleware<HttpCacheTestContract, string?>> Logger,
        HttpRequestCacheMiddleware<HttpCacheTestContract, string?> Middleware
    ) MiddlewareSetupNullable()
    {
        var context = new MockMediatorContext
        {
            Message = new HttpCacheTestContract("TestValue")
        };
        var cache = new MockCacheService(timeProvider);
        var contractKeyProvider = new DefaultContractKeyProvider(null!);
        var logger = TestHelpers.CreateLogger<HttpRequestCacheMiddleware<HttpCacheTestContract, string?>>(output);
        var middleware = new HttpRequestCacheMiddleware<HttpCacheTestContract, string?>(
            logger,
            timeProvider,
            cache,
            contractKeyProvider
        );

        return (context, cache, logger, middleware);
    }
}


public record HttpCacheTestContract(string Value) : IRequest<string>, IContractKey
{
    public string GetKey() => "TestKey";
}


public static class HttpCacheTestExtensions
{
    public static IMediatorContext SetHttp(this IMediatorContext context, HttpRequestMessage request, HttpResponseMessage response)
    {
        context.AddHeader("Http.Request", request);
        context.AddHeader("Http.Response", response);
        return context;
    }
}