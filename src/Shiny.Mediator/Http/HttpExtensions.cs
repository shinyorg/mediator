using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Http;

namespace Shiny.Mediator;


/// <summary>
/// Registration helpers for mediator HTTP handlers and middleware, plus
/// accessors for the HTTP request/response captured in <see cref="IMediatorContext"/>.
/// </summary>
public static class HttpExtensions
{
    /// <summary>
    /// Registers the HTTP client services needed by source-generated HTTP
    /// handlers. Called automatically as part of the standard middleware set.
    /// </summary>
    public static ShinyMediatorBuilder AddHttpClientServices(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.Services.AddMediatorHttpClientServices();
        return mediatorBuilder;
    }


    /// <summary>
    /// Opt-in registration for <see cref="HttpRequestCacheMiddleware{TRequest,TResult}"/>.
    /// Not part of the default middleware set — call this explicitly to enable
    /// <c>Cache-Control: max-age</c> response caching for HTTP-backed requests.
    /// </summary>
    public static ShinyMediatorBuilder AddHttpCacheMiddleware(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddOpenRequestMiddleware(typeof(HttpRequestCacheMiddleware<,>));
        return mediatorBuilder;
    }


    /// <summary>
    /// Registers <see cref="IHttpClientFactory"/> and the internal
    /// <c>HttpHandlerServices</c> singleton used by generated HTTP handlers.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    public static IServiceCollection AddMediatorHttpClientServices(this IServiceCollection services)
    {
        if (!services.Any(x => x.ImplementationType == typeof(HttpHandlerServices)))
        {
            services.AddHttpClient();
            services.AddSingleton<HttpHandlerServices>();
        }

        return services;
    }


    internal static IMediatorContext SetHttp(this IMediatorContext context, HttpRequestMessage request, HttpResponseMessage response)
    {
        context.AddHeader("Http.Request", request);
        context.AddHeader("Http.Response", response);
        return context;
    }

    /// <summary>
    /// Returns the <see cref="HttpRequestMessage"/> captured by the HTTP handler
    /// for the current mediator context, or <c>null</c> when the request did
    /// not go through an HTTP handler.
    /// </summary>
    public static HttpRequestMessage? GetHttpRequest(this IMediatorContext context)
        => context.TryGetValue<HttpRequestMessage?>("Http.Request");

    /// <summary>
    /// Returns the <see cref="HttpResponseMessage"/> captured by the HTTP handler
    /// for the current mediator context, or <c>null</c> when the request did
    /// not go through an HTTP handler.
    /// </summary>
    public static HttpResponseMessage? GetHttpResponse(this IMediatorContext context)
        => context.TryGetValue<HttpResponseMessage?>("Http.Response");
}