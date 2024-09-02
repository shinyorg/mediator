using System.Reflection;
using System.Text;
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

        var baseUri = http.Route.StartsWith("http")
            ? http.Route
            : configuration.GetSection("Mediator:Http")?["BaseUri"] ?? "http://localhost";
        
        var httpRequest = new HttpRequestMessage(http.Method, baseUri);
        var properties = request
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        var uri = baseUri + http.Route;
        foreach (var property in properties)
        {
            var parameter = property.GetCustomAttribute<HttpParameterAttribute>();
            if (parameter != null)
            {
                var propertyValue = property.GetValue(request);
                switch (parameter.Type)
                {
                    case HttpParameterType.Path:
                    case HttpParameterType.Query:
                        var value = propertyValue?.ToString() ?? String.Empty;
                        uri = uri.Replace("{" + property.Name + "}", value);
                        break;
                
                    case HttpParameterType.Header:
                        if (propertyValue != null)
                            httpRequest.Headers.Add(property.Name, propertyValue.ToString());
                        break;
                
                    case HttpParameterType.Body:
                        // TODO: file upload or form post?
                        if (httpRequest.Content != null)
                            throw new InvalidOperationException("Multiple body parameters not supported");
                        
                        var json = JsonSerializer.Serialize(propertyValue);
                        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
                        break;
                }
            }
        }

        httpRequest.RequestUri = new Uri(uri);
        foreach (var decorator in decorators)
        {
            await decorator
                .Decorate(httpRequest, request)
                .ConfigureAwait(false);
        }
        
        var response = await this.httpClient
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);
        
        response.EnsureSuccessStatusCode();
        TResult finalResult = default!;
        if (typeof(TResult) != typeof(Unit))
        {
            var stringResult = await response
                .Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            finalResult = JsonSerializer.Deserialize<TResult>(stringResult)!;
        }
        return finalResult!;
    }
}