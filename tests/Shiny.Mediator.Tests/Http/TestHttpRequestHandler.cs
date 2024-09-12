using System.Reflection;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Http;

namespace Shiny.Mediator.Tests;

public class TestHttpRequestHandler<TRequest, TResult>(
    IConfiguration configuration, 
    IEnumerable<IHttpRequestDecorator<TRequest, TResult>> decorators
) : HttpRequestHandler<TRequest, TResult>(configuration, decorators) 
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