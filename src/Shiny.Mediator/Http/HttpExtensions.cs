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
}