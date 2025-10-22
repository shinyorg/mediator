using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests;

public class TestHttpRequestHandler<TRequest, TResult>(
    ILogger<HttpRequestHandler<TRequest, TResult>> logger,
    IConfiguration configuration, 
    ISerializerService serializer,
    IHttpClientFactory httpClientFactory,
    IEnumerable<IHttpRequestDecorator> decorators
) : HttpRequestHandler<TRequest, TResult>(logger, configuration, serializer, httpClientFactory, decorators) 
    where TRequest : IHttpRequest<TResult>
{
    public HttpRequestMessage GetMessage(TRequest request, string testUri)
    {
        var http = request.GetType().GetCustomAttribute<HttpAttribute>();
        if (http == null)
            throw new InvalidOperationException("HttpAttribute not specified on request");
        
        var httpRequest = this.ContractToHttpRequest(request, http, testUri);
        return httpRequest;
    }
}