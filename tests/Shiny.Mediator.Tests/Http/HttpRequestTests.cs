using Microsoft.Extensions.Http;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class HttpRequestTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("atest", null, "https://test.com/this/is/atest")]
    [InlineData("atest", "1", "https://test.com/this/is/atest?query=1")]
    public void Test(string path, string? query, string expectedUri)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(expectedUri);
        
        var request = new MyHttpResultRequest
        {
            TheValue = path,
            QueryValue = query
        };
        var serializer = new SysTextJsonSerializerService();
        var logger = TestHelpers.CreateLogger<HttpRequestHandler<MyHttpResultRequest, HttpResult>>(output);
        var handler = new TestHttpRequestHandler<MyHttpResultRequest, HttpResult>(logger, null, serializer, null!, null!);
        var message = handler.GetMessage(request, "https://test.com");
        message.RequestUri!.ToString().ShouldBe(expectedUri);
    }
}

[Http(HttpVerb.Post, "/this/is/{TheValue}")]
public class MyHttpResultRequest : IHttpRequest<HttpResult>
{
    [HttpParameter(HttpParameterType.Path)]
    public string? TheValue { get; set; }
    
    [HttpParameter(HttpParameterType.Query, "query")]
    public string? QueryValue { get; set; }

}

// TODO: void, easy response
public class HttpResult;
