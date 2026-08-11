using System.Runtime.CompilerServices;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests;

/// <summary>
/// ILogger and IConfiguration are optional everywhere in the mediator - a container with neither
/// AddLogging() nor an IConfiguration registration must still dispatch, and any behaviour that is
/// driven by configuration simply turns itself off.
/// </summary>
public class OptionalDependencyTests
{
    static ServiceProvider BuildBareProvider(Action<ShinyMediatorBuilder>? configurator = null)
    {
        var services = new ServiceCollection();

        // deliberately NO AddLogging() and NO IConfiguration
        services.AddShinyMediator(configurator);
        services.AddSingletonAsImplementedInterfaces<BareHandler>();

        return services.BuildServiceProvider();
    }


    [Fact]
    public async Task Request_WithoutLoggingOrConfiguration()
    {
        var sp = BuildBareProvider();
        var (_, result) = await sp.GetRequiredService<IMediator>().Request(new BareRequest("hello"));
        result.ShouldBe("hello");
    }


    [Fact]
    public async Task Command_WithoutLoggingOrConfiguration()
    {
        var sp = BuildBareProvider();
        await sp.GetRequiredService<IMediator>().Send(new BareCommand());
        BareHandler.CommandExecuted.ShouldBeTrue();
    }


    [Fact]
    public async Task Event_WithoutLoggingOrConfiguration()
    {
        var sp = BuildBareProvider();
        await sp.GetRequiredService<IMediator>().Publish(new BareEvent());
        BareHandler.EventExecuted.ShouldBeTrue();
    }


    [Fact]
    public async Task Stream_WithoutLoggingOrConfiguration()
    {
        var sp = BuildBareProvider();
        var results = new List<string>();

        await foreach (var item in sp.GetRequiredService<IMediator>().Request(new BareStreamRequest()))
            results.Add(item.Result);

        results.ShouldBe(["one", "two"]);
    }


    [Fact]
    public async Task PerformanceLoggingMiddleware_WithoutLoggingOrConfiguration()
    {
        // the middleware reads its threshold from configuration - with no IConfiguration it must
        // pass the request straight through instead of blowing up on resolution
        var sp = BuildBareProvider(x => x.AddPerformanceLoggingMiddleware());
        var (_, result) = await sp.GetRequiredService<IMediator>().Request(new BareRequest("perf"));
        result.ShouldBe("perf");
    }


    [Fact]
    public void ContractKeyProvider_WithoutLogging()
    {
        var sp = BuildBareProvider();
        var key = sp.GetRequiredService<IContractKeyProvider>().GetContractKey(new BareRequest("key"));
        key.ShouldNotBeNullOrWhiteSpace();
    }
}


file record BareRequest(string Arg) : IRequest<string>;
file record BareStreamRequest : IStreamRequest<string>;
file record BareCommand : ICommand;
file record BareEvent : IEvent;

file class BareHandler :
    IRequestHandler<BareRequest, string>,
    IStreamRequestHandler<BareStreamRequest, string>,
    ICommandHandler<BareCommand>,
    IEventHandler<BareEvent>
{
    public static bool CommandExecuted { get; private set; }
    public static bool EventExecuted { get; private set; }

    public Task<string> Handle(BareRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(request.Arg);

    public async IAsyncEnumerable<string> Handle(
        BareStreamRequest request,
        IMediatorContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        yield return "one";
        await Task.Yield();
        yield return "two";
    }

    public Task Handle(BareCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        CommandExecuted = true;
        return Task.CompletedTask;
    }

    public Task Handle(BareEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        EventExecuted = true;
        return Task.CompletedTask;
    }
}
