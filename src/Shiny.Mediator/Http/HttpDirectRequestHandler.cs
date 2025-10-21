using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Http;


public class HttpDirectRequestHandler(
    ILogger<HttpDirectRequestHandler> logger,
    ISerializerService serializer,
    IHttpClientFactory httpClientFactory,
    IEnumerable<IHttpRequestDecorator> decorators,
    IConfiguration? configuration = null
) : IRequestHandler<HttpDirectRequest, object?>
{
    
    
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
    public async Task<object?> Handle(HttpDirectRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var url = this.GetUrl(request.ConfigNameOrRoute);
        
        if (request.Method == null)
        {
            var method = configuration?.GetValue($"Mediator:Http:Direct:{request.ConfigNameOrRoute}:Method", "get") ?? "get";
            request.Method = HttpMethod.Parse(method);
        }
        var httpRequest = new HttpRequestMessage(request.Method, url);
        this.PopulateContent(httpRequest, request);
        
        foreach (var header in request.Headers)
            httpRequest.Headers.Add(header.Key, header.Value);

        await this.Decorate(context, httpRequest, cancellationToken).ConfigureAwait(false);

        var cts = new CancellationTokenSource();
        await using var _ = cancellationToken.Register(() => cts.Cancel());
        if (request.Timeout != null)
            cts.CancelAfter(request.Timeout.Value);

        var httpClient = httpClientFactory.CreateClient();
        var httpResponse = await httpClient
            .SendAsync(httpRequest, cts.Token)
            .ConfigureAwait(false);
        
        context.SetHttp(httpRequest, httpResponse);

        await this.WriteDebugIfEnable(httpRequest, httpResponse, cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

        object? result = null;
        if (request.ResultType != null)
        {
            var stringContent = await httpResponse.Content.ReadAsStringAsync(cts.Token);
            if (!String.IsNullOrWhiteSpace(stringContent))
                result = serializer.Deserialize(stringContent, request.ResultType);
        }
        return result;
    }


    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
    string GetUrl(string routeName)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(routeName);
        string? url = null;
        
        if (routeName.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            routeName.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
        {
            url = routeName;
        }
        else
        {
            if (configuration == null)
                throw new InvalidOperationException("IConfiguration is null");
            
            url = configuration.GetValue<string>($"Mediator:Http:Direct:{routeName}:Url");
            if (url == null)
            {
                var baseUrl = configuration.GetValue<string>("Mediator:Http:Direct:BaseUrl");
                if (baseUrl != null)
                {
                    url = baseUrl.TrimEnd('/');
                    if (routeName.StartsWith('/'))
                        url += routeName;
                    else
                        url = $"{baseUrl}/{routeName}";
                }
            }
        } 
        if (String.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException($"No URL set for route '{routeName}'");
        
        logger.LogDebug("URL: {url}", url);
        return url;
    }
    
    
    async Task Decorate(IMediatorContext context, HttpRequestMessage httpRequest, CancellationToken cancellationToken)
    {
        foreach (var decorator in decorators)
        {
            logger.LogDebug("Decorating {Type}", decorator.GetType().Name);
            await decorator
                .Decorate(httpRequest, context, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    
    void PopulateContent(HttpRequestMessage httpRequest, HttpDirectRequest request)
    {
        if (request.UploadFiles.Count == 0)
        {
            httpRequest.Content = this.TryGetDataContent(request);
        }
        else
        {
            logger.LogDebug("Sending Multipart Content");
            var multipart = new MultipartFormDataContent();
            this.PopulateUploadFiles(multipart, request);
            var data = this.TryGetDataContent(request);
            if (data != null)
                multipart.Add(data, "data");
        }
    }
    

    void PopulateUploadFiles(MultipartFormDataContent multipart, HttpDirectRequest request)
    {
        for (var i = 0; i < request.UploadFiles.Count; i++)
        {
            var file = request.UploadFiles[i];
            if (!file.Exists)
                throw new InvalidOperationException($"UploadFile {file.FullName} does not exist");
            
            var streamContent = new StreamContent(file.OpenRead());
            streamContent.Headers.ContentDisposition!.FileNameStar = null;
            multipart.Add(streamContent, "file_" + i, file.Name);            
        }
    }
    
    
    HttpContent? TryGetDataContent(HttpDirectRequest request)
    {
        if (request is { SerializableBody: not null, FormValues.Count: > 0 })
            throw new InvalidOperationException("You cannot ship form values and json body at the same time");

        if (request.SerializableBody != null)
        {
            var json = serializer.Serialize(request.SerializableBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return content;
        }

        if (request.FormValues.Count > 0)
            return new FormUrlEncodedContent(request.FormValues);

        return null;
    }
    

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue<int> is safe for trimming")]
    async ValueTask WriteDebugIfEnable(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (configuration == null)
            return;
        
        var debug = configuration.GetValue<bool>("Mediator:Http:Debug");
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
        
        var responseBody = await response
            .Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        
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
        {
            logger.LogInformation("Request Body: {Body}", requestBody);
            logger.LogInformation("Response Body: {Body}", responseBody);
        }
    }
}