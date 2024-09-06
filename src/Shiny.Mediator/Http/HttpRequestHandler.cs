using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
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
        
        var baseUri = this.GetBaseUri(request, http);
        var httpRequest = this.ContractToHttpRequest(request, http, baseUri);
        await this.Decorate(request, httpRequest).ConfigureAwait(false);

        var result = await this.Send(httpRequest, cancellationToken).ConfigureAwait(false);
        return result;
    }


    protected virtual async Task<TResult> Send(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
    {
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
    

    protected virtual async Task Decorate(TRequest request, HttpRequestMessage httpRequest)
    {
        foreach (var decorator in decorators)
        {
            await decorator
                .Decorate(httpRequest, request)
                .ConfigureAwait(false);
        }
    }
    
    
    protected static HttpMethod ToMethod(HttpVerb verb) => verb switch 
    {
        HttpVerb.Get => HttpMethod.Get,
        HttpVerb.Post => HttpMethod.Post,
        HttpVerb.Put => HttpMethod.Put,
        HttpVerb.Delete => HttpMethod.Delete,
        HttpVerb.Patch => HttpMethod.Patch,
        _ => throw new NotSupportedException("HTTP Verb not supported: " + verb)
    };
    
    
    protected virtual HttpRequestMessage ContractToHttpRequest(TRequest request, HttpAttribute attribute, string baseUri)
    {
        var httpMethod = ToMethod(attribute.Verb);
        var httpRequest = new HttpRequestMessage(httpMethod, baseUri);
        
        var properties = request
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToList();

        var uri = baseUri + attribute.Route;
        foreach (var property in properties)
        {
            var parameter = property.GetCustomAttribute<HttpParameterAttribute>();
            if (parameter != null)
            {
                var propertyValue = property.GetValue(request)?.ToString();
                
                switch (parameter.Type)
                {
                    case HttpParameterType.Path:
                        if (propertyValue != null)
                            throw new InvalidOperationException($"Path parameter '{property.Name}' not set");
                           
                        var epath = HttpUtility.UrlEncode(propertyValue);
                        uri = uri.Replace("{" + property.Name + "}", epath);
                        break;
                    
                    case HttpParameterType.Query:
                        if (propertyValue != null)
                        {
                            var eq = HttpUtility.UrlEncode(propertyValue);
                            uri += uri.Contains('?') ? "&" : "?";
                            uri += $"{property.Name}={eq}";
                        }

                        break;
                
                    case HttpParameterType.Header:
                        if (propertyValue != null)
                            httpRequest.Headers.Add(property.Name, propertyValue);
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
        return httpRequest;
    }
    

    protected virtual string GetBaseUri(TRequest request, HttpAttribute attribute)
    {
        var baseUri = attribute.Route.StartsWith("http")
            ? attribute.Route
            : configuration.GetSection("Mediator:Http")?["BaseUri"] ?? "http://localhost";

        return baseUri;
    }
}