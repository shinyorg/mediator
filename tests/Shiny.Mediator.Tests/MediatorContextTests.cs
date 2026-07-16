using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


/// <summary>
/// Regression tests for IMediatorContext behaviour - child context tracking and background publishing.
/// </summary>
public class MediatorContextTests(ITestOutputHelper output)
{
    ServiceProvider Build(
        Action<IServiceCollection> configure,
        Action<ShinyMediatorBuilder>? mediatorConfigure = null
    )
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        services.AddConfiguration();

        // exception handlers must be registered through the builder - AddShinyMediator runs the
        // configurator before installing the standard middleware, which includes PreventEventExceptions
        services.AddShinyMediator(mediatorConfigure);
        configure(services);
        return services.BuildServiceProvider();
    }


    [Fact]
    public void ChildContexts_ReturnsSnapshot_NotTheLiveList()
    {
        var sp = this.Build(_ => { });
        using var scope = sp.CreateScope();

        var context = new MediatorContext(
            scope,
            new BackgroundEvent(),
            null,
            sp.GetRequiredService<IMediatorDirector>()
        );

        var snapshot = context.ChildContexts;
        snapshot.Count.ShouldBe(0);

        context.CreateChild(null, true);

        // the snapshot must not observe the new child - handing out the live list means a caller
        // enumerating it while parallel event publishing adds children throws
        snapshot.Count.ShouldBe(0);
        context.ChildContexts.Count.ShouldBe(1);
    }


    [Fact]
    public async Task PublishToBackground_FromContext_UsesItsOwnScope()
    {
        var gate = new BackgroundGate();
        var sp = this.Build(x =>
        {
            x.AddSingleton(gate);
            x.AddScoped<ScopedProbe>();
            x.AddSingleton<IRequestHandler<TriggerRequest, string>, TriggerRequestHandler>();
            x.AddSingleton<IEventHandler<BackgroundEvent>, BackgroundEventHandler>();
        });

        var (_, result) = await sp.GetRequiredService<IMediator>().Request(new TriggerRequest());
        result.ShouldBe("done");

        // the outer request has returned, so the scope it dispatched under is disposed. Release the
        // background handler only now - it must still be able to resolve scoped services.
        gate.OuterDispatchCompleted.SetResult();
        await gate.HandlerFinished.Task.WaitAsync(TimeSpan.FromSeconds(10));

        gate.HandlerError.ShouldBeNull();
        gate.ResolvedScopedService.ShouldBeTrue();
    }


    [Fact]
    public async Task PublishToBackground_FromContext_InvokesExceptionHandler()
    {
        var gate = new BackgroundGate();
        var sp = this.Build(
            x =>
            {
                x.AddSingleton(gate);
                x.AddSingleton<IRequestHandler<ThrowingTriggerRequest, string>, ThrowingTriggerRequestHandler>();
                x.AddSingleton<IEventHandler<ThrowingBackgroundEvent>, ThrowingBackgroundEventHandler>();
            },
            mediator => mediator.AddExceptionHandler<GateExceptionHandler>()
        );

        var (_, result) = await sp.GetRequiredService<IMediator>().Request(new ThrowingTriggerRequest());
        result.ShouldBe("done");

        // a background publish from a context used to swallow handler exceptions entirely, while the
        // same call on IMediator ran the exception handler chain
        await gate.HandlerFinished.Task.WaitAsync(TimeSpan.FromSeconds(10));
        gate.HandlerError.ShouldBeOfType<InvalidOperationException>();
    }
}


// ── Test helpers ────────────────────────────────────────────────────────

file class BackgroundGate
{
    public TaskCompletionSource OuterDispatchCompleted { get; } = new();
    public TaskCompletionSource HandlerFinished { get; } = new();
    public Exception? HandlerError { get; set; }
    public bool ResolvedScopedService { get; set; }
}

file class ScopedProbe;

file record TriggerRequest : IRequest<string>;

file class TriggerRequestHandler : IRequestHandler<TriggerRequest, string>
{
    public Task<string> Handle(TriggerRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        context.PublishToBackground(new BackgroundEvent());
        return Task.FromResult("done");
    }
}

file record BackgroundEvent : IEvent;

file class BackgroundEventHandler(BackgroundGate gate) : IEventHandler<BackgroundEvent>
{
    public async Task Handle(BackgroundEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        try
        {
            // don't touch DI until the dispatch that fired this event is fully unwound
            await gate.OuterDispatchCompleted.Task.ConfigureAwait(false);

            context.ServiceScope.ServiceProvider.GetRequiredService<ScopedProbe>();
            gate.ResolvedScopedService = true;
        }
        catch (Exception ex)
        {
            gate.HandlerError = ex;
        }
        finally
        {
            gate.HandlerFinished.TrySetResult();
        }
    }
}


file record ThrowingTriggerRequest : IRequest<string>;

file class ThrowingTriggerRequestHandler : IRequestHandler<ThrowingTriggerRequest, string>
{
    public Task<string> Handle(ThrowingTriggerRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        context.PublishToBackground(new ThrowingBackgroundEvent());
        return Task.FromResult("done");
    }
}

file record ThrowingBackgroundEvent : IEvent;

file class ThrowingBackgroundEventHandler : IEventHandler<ThrowingBackgroundEvent>
{
    public Task Handle(ThrowingBackgroundEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Test exception from background handler");
}

file class GateExceptionHandler(BackgroundGate gate) : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        gate.HandlerError = exception;
        gate.HandlerFinished.TrySetResult();
        return Task.FromResult(true);
    }
}
