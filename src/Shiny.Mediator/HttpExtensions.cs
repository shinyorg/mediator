using Shiny.Mediator.Http;

namespace Shiny.Mediator;


public static class HttpExtensions
{
    
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
    
    
    /// <summary>
    /// Allows direct HTTP requests to work in a strong type fashion
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request"></param>
    /// <param name="configNameOrRoute"></param>
    /// <param name="method"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<(IMediatorContext Context, TResult Result)> DirectHttpRequest<TResult>(
        this IMediator mediator, 
        IRequest<TResult> request,
        string configNameOrRoute,
        HttpMethod? method = null,
        Action<HttpDirectRequest>? configure = null
    )
    {
        var httpRequest = new HttpDirectRequest
        {
            ConfigNameOrRoute = configNameOrRoute,
            Method = method,
            SerializableBody = request,
            ResultType = typeof(TResult)
        };
        configure?.Invoke(httpRequest);
        
        var response = await mediator.Request(httpRequest).ConfigureAwait(false);
        var result = (TResult)response.Result!;
        return (response.Context, result);
    }    
}