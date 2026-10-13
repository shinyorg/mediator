using Shiny.Mediator.Http;
using Shiny.Mediator.Tests.Mocks;

namespace Shiny.Mediator.Tests;


public class HttpRequestCancellationTests
{
    [Fact(DisplayName = "HTTP Send - caller cancel throws TaskCanceledException instead of returning default (#60)")]
    public async Task CallerCancel_Throws()
    {
        var handler = new CancelTestHttpHandler();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<TaskCanceledException>(
            () => handler.Run(TimeSpan.FromSeconds(30), cts.Token)
        );
    }


    [Fact(DisplayName = "HTTP Send - internal timeout still surfaces as TimeoutException")]
    public async Task InternalTimeout_ThrowsTimeoutException()
    {
        var handler = new CancelTestHttpHandler();

        await Should.ThrowAsync<TimeoutException>(
            () => handler.Run(TimeSpan.FromMilliseconds(100), CancellationToken.None)
        );
    }
}


file class CancelTestHttpHandler() : BaseHttpRequestHandler(new HttpHandlerServices(
    TestHelpers.CreateTestSerializer(),
    new NeverAnswersHttpClientFactory(),
    []
))
{
    public Task<string> Run(TimeSpan timeout, CancellationToken cancellationToken) => this.Send<string>(
        new MockMediatorContext(),
        new HttpRequestMessage(HttpMethod.Get, "http://never-answers.invalid/slow"),
        timeout,
        cancellationToken
    );
}


file class NeverAnswersHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new NeverAnswersHandler());
}


// mirrors the issue repro - a primary handler that only ever completes by cancellation
file class NeverAnswersHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new InvalidOperationException("unreachable");
    }
}
