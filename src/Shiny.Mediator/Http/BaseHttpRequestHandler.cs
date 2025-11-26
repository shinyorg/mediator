using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Http;


public record HttpHandlerServices(
    ILoggerFactory LoggerFactory,
    IConfiguration Configuration,
    ISerializerService Serializer,
    IHttpClientFactory HttpClientFactory,
    IEnumerable<IHttpRequestDecorator> Decorators
);

public abstract class BaseHttpRequestHandler(HttpHandlerServices services)
{
    readonly ILogger logger = services.LoggerFactory.CreateLogger<BaseHttpRequestHandler>();


    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
    protected virtual async IAsyncEnumerable<TResult> HandleStream<TRequest, TResult>(
        HttpRequestMessage httpRequest,
        TRequest request, 
        bool useServerSentEvents,
        IMediatorContext context, 
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var httpClient = services.HttpClientFactory.CreateClient();
        await this.Decorate(context, httpRequest, cancellationToken).ConfigureAwait(false);
        
        var httpResponse = await httpClient
            .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
       
        context.SetHttp(httpRequest, httpResponse);
        await this.WriteDebugIfEnable(httpRequest, httpResponse, cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();
       
        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        if (useServerSentEvents)
        {
            await foreach (var sseEvent in ReadServerSentEvents<TResult>(responseStream, cancellationToken))
            {
                yield return sseEvent;
            }

        }
        else
        {
            await foreach (var obj in services.Serializer.DeserlializeAsyncEnumerable<TResult>(responseStream, cancellationToken))
            { 
                yield return obj;
            }
        }
    }
    
    
    protected virtual async IAsyncEnumerable<T> ReadServerSentEvents<T>(
        Stream stream, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var sb = new StringBuilder();
        
        while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (String.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0)
                {
                    var json = sb.ToString();
                    sb.Clear();
                    yield return services.Serializer.Deserialize<T>(json);
                }
            }
            else if (line.StartsWith("data:"))
            {
                sb.AppendLine(line.Substring("data:".Length).Trim());
            }
        }
        // Flush any remaining data
        // if (sb.Length > 0)
        // {
        //     yield return sb.ToString();
        // }
    }
    
    
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
    protected virtual async Task<TResult> HandleRequest<TRequest, TResult>(
        HttpRequestMessage httpRequest,
        TRequest request, 
        IMediatorContext context, 
        CancellationToken cancellationToken
    )
    {
        await this.Decorate(context, httpRequest, cancellationToken).ConfigureAwait(false);
        var timeoutSeconds = services.Configuration.GetValue("Mediator:Http:Timeout", 20);
        
        var result = await this
            .Send<TResult>(context, httpRequest, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)
            .ConfigureAwait(false);

        return result;
    }


    protected virtual HttpRequestMessage CreateHttpRequest(IMediatorContext context, HttpMethod httpMethod, string parsedUrl)
    {
        var url = parsedUrl;
        if (!parsedUrl.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) &&
            !parsedUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
        {
            var baseUri = this.GetBaseUri(context.Message);
            if (!baseUri.EndsWith("/"))
                baseUri += "/";

            url = baseUri + parsedUrl.TrimStart('/');
        }

        return new HttpRequestMessage(httpMethod, url);
    }

    
        // var route = "/ping";

        // return this.HandleRequest<global::TestApi.Ping, global::System.Net.Http.HttpResponseMessage>(
        //     global::System.Net.Http.HttpMethod.Post,
        //     route,
        //     request,
        //     context,
        //     cancellationToken
        // );

    
    protected virtual async Task<TResult> Send<TResult>(
        IMediatorContext context,
        HttpRequestMessage httpRequest, 
        TimeSpan timeout, 
        CancellationToken cancellationToken
    )
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(timeout);
        await using var _ = cancellationToken.Register(() => cts.Cancel());
    
        TResult finalResult = default!;
        try
        {
            var httpClient = services.HttpClientFactory.CreateClient();
            var response = await httpClient
                .SendAsync(httpRequest, cts.Token)
                .ConfigureAwait(false);
    
            await this
                .WriteDebugIfEnable(httpRequest, response, cts.Token)
                .ConfigureAwait(false);
    
            context.SetHttp(httpRequest, response);
            response.EnsureSuccessStatusCode();
    
            if (typeof(TResult) == typeof(HttpResponseMessage))
            {
                finalResult = (TResult)(object)response;
            }
            else
            {
                var stringResult = await response
                    .Content
                    .ReadAsStringAsync(cts.Token)
                    .ConfigureAwait(false);
    
                finalResult = services.Serializer.Deserialize<TResult>(stringResult)!;
            }
        }
        catch (TaskCanceledException ex)
        {
            if (!cancellationToken.IsCancellationRequested)
                throw new TimeoutException("HTTP Request timed out", ex);
        }
        return finalResult;
    }

    
    protected virtual async Task Decorate(IMediatorContext context, HttpRequestMessage httpRequest, CancellationToken cancellationToken)
    {
        foreach (var decorator in services.Decorators)
        {
            this.logger.LogDebug("Decorating {Type}", decorator.GetType().Name);
            await decorator
                .Decorate(httpRequest, context, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
    protected virtual async ValueTask WriteDebugIfEnable(
        HttpRequestMessage request, 
        HttpResponseMessage response, 
        CancellationToken cancellationToken
    )
    {
        var debug = services.Configuration.GetValue("Mediator:Http:Debug", false);
        if (!debug)
            return;
    
        var requestBody = String.Empty;
        if (request.Content != null)
        {
            requestBody = await request
                .Content!
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);    
        }

        // var responseBody = await response
        //     .Content
        //     .ReadAsStringAsync(cancellationToken)
        //     .ConfigureAwait(false);
        
        var details = new Dictionary<string, object>
        {
            ["Method"] = request.Method.Method,
            ["Url"] = request.RequestUri?.ToString() ?? "",
            ["StatusCode"] = response.StatusCode,
            ["StatusDescription"] = response
        };
        foreach (var header in request.Headers)
            details.Add("Request_" + header.Key, header.Value);
        
        foreach (var header in response.Headers)
            details.Add("Response_" + header.Key, header.Value);
        
        using (logger.BeginScope(details))
            logger.LogInformation("Request Body: {Body}", requestBody);
    }

    
    protected virtual string GetBaseUri(object request)
    {
        var cfg = services.Configuration.GetHandlerSection("Http", request, this);
        if (cfg == null)
            throw new InvalidOperationException("No base URI configured for: " + request.GetType().FullName);
        
        if (String.IsNullOrWhiteSpace(cfg.Value))
            throw new InvalidOperationException("Base URI empty for: " + request.GetType().FullName);
        
        logger.LogDebug("Base URI: {Uri}", cfg.Value);
        return cfg.Value;
    }
}

 // protected static HttpMethod ToMethod(HttpVerb verb) => verb switch 
    // {
    //     HttpVerb.Get => HttpMethod.Get,
    //     HttpVerb.Post => HttpMethod.Post,
    //     HttpVerb.Put => HttpMethod.Put,
    //     HttpVerb.Delete => HttpMethod.Delete,
    //     HttpVerb.Patch => HttpMethod.Patch,
    //     _ => throw new NotSupportedException("HTTP Verb not supported: " + verb)
    // };
    //
    //
    // protected internal virtual HttpRequestMessage ContractToHttpRequest(
    //     TRequest request, 
    //     HttpAttribute attribute,
    //     string baseUri
    // )
    // {
    //     var httpMethod = ToMethod(attribute.Verb);
    //     logger.LogDebug("HTTP Method: {HttpMethod}", httpMethod);
    //     
    //     var httpRequest = new HttpRequestMessage(httpMethod, baseUri);
    //     var properties = request
    //         .GetType()
    //         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //         .ToList();
    //
    //     // TODO: headers to http headers - think on this one
    //     // foreach (var header in context.Values)
    //     // {
    //     //     if (header.Value is string)
    //     //     {
    //     //         
    //     //     }
    //     // }
    //     var uri = baseUri.TrimEnd('/');
    //     if (attribute.Route.StartsWith('/'))
    //         uri += attribute.Route;
    //     else
    //         uri = $"{uri}/{attribute.Route}";
    //     
    //     foreach (var property in properties)
    //     {
    //         var parameter = property.GetCustomAttribute<HttpParameterAttribute>();
    //         if (parameter != null)
    //         {
    //             var parameterName = parameter.ParameterName ?? property.Name;
    //             var propertyValue = property.GetValue(request);
    //             
    //             switch (parameter.Type)
    //             {
    //                 case HttpParameterType.Path:
    //                     if (propertyValue == null)
    //                         throw new InvalidOperationException($"Path parameters cannot be null - '{parameterName}'");
    //                        
    //                     var epath = HttpUtility.UrlEncode(propertyValue.ToString());
    //                     uri = uri.Replace("{" + parameterName + "}", epath);
    //                     break;
    //                 
    //                 case HttpParameterType.Query:
    //                     if (propertyValue != null)
    //                     {
    //                         var eq = HttpUtility.UrlEncode(propertyValue.ToString());
    //                         uri += uri.Contains('?') ? "&" : "?";
    //                         uri += $"{parameterName}={eq}";
    //                     }
    //                     break;
    //             
    //                 case HttpParameterType.Header:
    //                     if (propertyValue != null)
    //                     {
    //                         var headerValue = propertyValue.ToString();
    //                         logger.LogDebug($"Header: {parameterName} - Value: {headerValue}");
    //                         httpRequest.Headers.Add(parameterName, headerValue);
    //                     }
    //                     break;
    //             
    //                 case HttpParameterType.Body:
    //                     // TODO: file upload or form post?
    //                     if (httpRequest.Content != null)
    //                         throw new InvalidOperationException("Multiple body parameters not supported");
    //
    //                     var json = serializer.Serialize(propertyValue);
    //                     httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
    //                     logger.LogDebug("HTTP Body: " + json);
    //                     break;
    //             }
    //         }
    //     }
    //
    //     if (uri.Contains("{") && uri.Contains("}"))
    //         throw new InvalidOperationException("Not all route parameters satisfied");
    //     
    //     logger.LogDebug($"URI: {uri}");
    //     httpRequest.RequestUri = new Uri(uri);
    //     return httpRequest;
    // }