using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class TimerRefreshStreamRequestMiddlewareTests
{
    readonly ITestOutputHelper output;
    readonly ConfigurationManager config = new();
    readonly MockMediatorContext context = new()
    {
        Message = new TestTimerStreamRequest(),
        MessageHandler = new TestTimerStreamRequestHandler()
    };
    readonly TimerRefreshStreamRequestMiddleware<TestTimerStreamRequest, int> middleware;

    public TimerRefreshStreamRequestMiddlewareTests(ITestOutputHelper output)
    {
        this.output = output;
        var logger = TestHelpers.CreateLogger<TimerRefreshStreamRequestMiddleware<TestTimerStreamRequest, int>>(output);
        this.middleware = new TimerRefreshStreamRequestMiddleware<TestTimerStreamRequest, int>(logger, this.config);
    }
    
    
    [Fact]
    public async Task E2e_Test()
    {
        var del = new StreamRequestHandlerDelegate<int>(() => this.TestStream(1, 1, CancellationToken.None));
        using var cts = new CancellationTokenSource();
        await foreach (var item in this.middleware.Process(context, del, cts.Token))
        {
            item.ShouldBe(1);
            await cts.CancelAsync();
        }
    }
    
    
    [Fact]
    public async Task MediatorContext_Header_Test()
    {
        this.context.SetTimerRefresh(5);
        var del = new StreamRequestHandlerDelegate<int>(() => this.TestStream(1, 1, CancellationToken.None));
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(11));
        
        var count = 0;
        try
        {
            await foreach (var item in this.middleware.Process(context, del, cts.Token))
            {
                count++;
                output.WriteLine($"Count: {count} - Item: {item}");
            }
        }
        catch (TaskCanceledException)
        {
            output.WriteLine("Cancel Received");
        }

        // initial stream will fire, followed by 2 timer triggers
        count.ShouldBe(2);
    }

    
    // configuration and attributes are tested elsewhere
    // [Fact]
    // public async Task Configuration_Test()
    // {
    //     this.config.AddInMemoryCollection([
    //         new KeyValuePair<string, string?>("handler", "handler")
    //     ]);
    //     var middleware = new TimerRefreshStreamRequestMiddleware<TestStreamRequest, int>(config);
    //     
    //     var del = new StreamRequestHandlerDelegate<int>(() => this.TestStream(1, 1, CancellationToken.None));
    //     using var cts = new CancellationTokenSource();
    //     await foreach (var item in middleware.Process(context, del, cts.Token))
    //     {
    //     }
    // }
    
    
    async IAsyncEnumerable<int> TestStream(int repeatCount, int gapSeconds, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < repeatCount; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(gapSeconds), cancellationToken);
            yield return repeatCount;
        }
    }
}

class TestTimerStreamRequest : IStreamRequest<int>;

class TestTimerStreamRequestHandler : IStreamRequestHandler<TestTimerStreamRequest, int>
{
    public IAsyncEnumerable<int> Handle(TestTimerStreamRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}