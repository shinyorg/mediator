using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Http;

namespace Shiny.Mediator;


public static class HttpExtensions
{
    /// <summary>
    /// Add HTTP Client to mediator
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddHttpClientServices(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.Services.AddMediatorHttpClientServices();
        return mediatorBuilder;
    }


    /// <summary>
    /// Add HTTP Request Cache Middleware to mediator
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddHttpCacheMiddleware(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddOpenRequestMiddleware(typeof(HttpRequestCacheMiddleware<,>));
        return mediatorBuilder;
    }


    /// <summary>
    /// Add HTTP Client to mediator
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
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
    
    public static HttpRequestMessage? GetHttpRequest(this IMediatorContext context)
        => context.TryGetValue<HttpRequestMessage?>("Http.Request");
    
    public static HttpResponseMessage? GetHttpResponse(this IMediatorContext context)
        => context.TryGetValue<HttpResponseMessage?>("Http.Response");
}