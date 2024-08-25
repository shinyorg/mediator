using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator.Http;

public class HttpRequestHandler<TRequest, TResult>(
    IConfiguration configuration,
    IEnumerable<IHttpRequestDecorator<TRequest, TResult>> decorators
) : IRequestHandler<TRequest, TResult> where TRequest : IHttpRequest<TResult>
{
    readonly HttpClient httpClient = new();
    
    public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var http = request.GetType().GetCustomAttribute<HttpAttribute>();
        if (http == null)
            throw new InvalidOperationException("HttpAttribute not specified on request");

        var baseUri = configuration.GetSection("Mediator:Http")?["BaseUri"];
        var httpRequest = new HttpRequestMessage(http.Method, baseUri + http.Route);
        foreach (var decorator in decorators)
        {
            await decorator
                .Decorate(httpRequest, request)
                .ConfigureAwait(false);
        }
        // if (http.IsAuthRequired)
        // {
        //     var success = await authService.TryRefresh();
        //     if (!success)
        //         throw new InvalidOperationException("Failed to refresh token");
        //
        //     httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authService.Auth!.Token);
        // }
    
        var response = await this.httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);
        
        response.EnsureSuccessStatusCode();
        var stringResult = await response
            .Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        
        var result = JsonSerializer.Deserialize<TResult>(stringResult);
    
        return result!;
    }
}