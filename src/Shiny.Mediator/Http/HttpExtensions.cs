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
        if (!mediatorBuilder.Services.Any(x => x.ImplementationType == typeof(HttpHandlerServices)))
        {
            mediatorBuilder.Services.AddHttpClient();
            mediatorBuilder.Services.AddSingleton<HttpHandlerServices>();
        }
        return mediatorBuilder;
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