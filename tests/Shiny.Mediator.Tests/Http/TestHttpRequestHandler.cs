using System.Reflection;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests;

public class TestHttpRequestHandler<TRequest, TResult>(
    IConfiguration configuration, 
    ISerializerService serializer,
    IEnumerable<IHttpRequestDecorator<TRequest, TResult>> decorators
) : HttpRequestHandler<TRequest, TResult>(configuration, serializer, decorators) 
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