using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Http;


public class HttpRequestHandler<TRequest, TResult>(
    ILogger<HttpRequestHandler<TRequest, TResult>> logger,
    IConfiguration configuration,
    ISerializerService serializer,
    IEnumerable<IHttpRequestDecorator<TRequest, TResult>> decorators
) : IRequestHandler<TRequest, TResult> where TRequest : IHttpRequest<TResult>
{
    readonly HttpClient httpClient = new();
    
    
    public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var http = request.GetType().GetCustomAttribute<HttpAttribute>();
        if (http == null)
            throw new InvalidOperationException("HttpAttribute not specified on request");
        
        var baseUri = this.GetBaseUri(request);
        logger.LogDebug("Base URI: " + baseUri);
        
        var httpRequest = this.ContractToHttpRequest(request, http, baseUri);
        await this.Decorate(request, httpRequest).ConfigureAwait(false);

        var result = await this.Send(httpRequest, http.Timeout, cancellationToken).ConfigureAwait(false);
        return result;
    }
    

    protected virtual async Task<TResult> Send(HttpRequestMessage httpRequest, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(timeout);
        await using var _ = cancellationToken.Register(() => cts.Cancel());

        try
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

                logger.LogDebug("Raw Result: " + stringResult);
                finalResult = serializer.Deserialize<TResult>(stringResult)!;
            }

            return finalResult!;
        }
        catch (TaskCanceledException ex)
        {
            if (!cancellationToken.IsCancellationRequested)
                throw new TimeoutException("HTTP Request timed out", ex);
        }
    }
    

    protected virtual async Task Decorate(TRequest request, HttpRequestMessage httpRequest)
    {
        foreach (var decorator in decorators)
        {
            logger.LogDebug("Decorating " + decorator.GetType().Name);
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
        logger.LogDebug("HTTP Method: " + httpMethod);
        
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
                var parameterName = parameter.ParameterName ?? property.Name;
                var propertyValue = property.GetValue(request)?.ToString();
                
                switch (parameter.Type)
                {
                    case HttpParameterType.Path:
                        if (propertyValue == null)
                            throw new InvalidOperationException($"Path parameters cannot be null - '{parameterName}'");
                           
                        var epath = HttpUtility.UrlEncode(propertyValue);
                        uri = uri.Replace("{" + parameterName + "}", epath);
                        break;
                    
                    case HttpParameterType.Query:
                        if (propertyValue != null)
                        {
                            var eq = HttpUtility.UrlEncode(propertyValue);
                            uri += uri.Contains('?') ? "&" : "?";
                            uri += $"{parameterName}={eq}";
                        }
                        break;
                
                    case HttpParameterType.Header:
                        if (propertyValue != null)
                        {
                            logger.LogDebug($"Header: {parameterName} - Value: {propertyValue}");
                            httpRequest.Headers.Add(parameterName, propertyValue);
                        }
                        break;
                
                    case HttpParameterType.Body:
                        // TODO: file upload or form post?
                        if (httpRequest.Content != null)
                            throw new InvalidOperationException("Multiple body parameters not supported");
                        
                        var json = JsonSerializer.Serialize(propertyValue);
                        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
                        logger.LogDebug("HTTP Body: " + json);
                        break;
                }
            }
        }

        if (uri.Contains("{") && uri.Contains("}"))
            throw new InvalidOperationException("Not all route parameters satisfied");
        
        logger.LogDebug($"URI: {uri}");
        httpRequest.RequestUri = new Uri(uri);
        return httpRequest;
    }
    

    protected virtual string GetBaseUri(TRequest request)
    {
        var cfg = configuration.GetHandlerSection("Http", request, this);
        if (cfg == null)
            throw new InvalidOperationException("No base URI configured for: " + request.GetType().FullName);
        
        if (String.IsNullOrWhiteSpace(cfg.Value))
            throw new InvalidOperationException("Base URI empty for: " + request.GetType().FullName);
        
        logger.LogDebug("Base URI: " + cfg.Value);
        return cfg.Value;
    }
}