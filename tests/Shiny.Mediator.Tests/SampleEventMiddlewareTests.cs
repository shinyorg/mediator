using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class SampleEventMiddlewareTests
{
    readonly ConfigurationManager config = new();
    readonly SampleEventMiddleware<SampleTestEvent> middleware;

    public SampleEventMiddlewareTests(ITestOutputHelper output)
    {
        var logger = TestHelpers.CreateLogger<SampleEventMiddleware<SampleTestEvent>>(output);
        this.middleware = new SampleEventMiddleware<SampleTestEvent>(logger, this.config);
    }

    [Fact]
    public async Task NoSample_ExecutesImmediately()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new NoSampleEventHandler()
        };

        var executed = false;
        await this.middleware.Process(
            context,
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Sample_DelaysExecution()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        var executed = false;
        await this.middleware.Process(
            context,
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Should not be executed immediately
        executed.ShouldBeFalse();

        // Wait for sample window to pass
        await Task.Delay(200);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Sample_LastEventWins()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        var executionCount = 0;
        var lastValue = 0;

        // Fire multiple events in rapid succession
        for (var i = 1; i <= 5; i++)
        {
            var capturedI = i;
            await this.middleware.Process(
                context,
                () =>
                {
                    executionCount++;
                    lastValue = capturedI;
                    return Task.CompletedTask;
                },
                CancellationToken.None
            );
            await Task.Delay(10); // Small delay between events, but less than sample window
        }

        // Wait for sample window to complete
        await Task.Delay(200);

        // Only the last event should have been executed
        executionCount.ShouldBe(1);
        lastValue.ShouldBe(5);
    }

    [Fact]
    public async Task Sample_FixedWindow_TimerDoesNotReset()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        var executed = false;

        // First event starts the 100ms timer
        await this.middleware.Process(
            context,
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Wait 60ms, then send another event - timer should NOT reset
        await Task.Delay(60);
        await this.middleware.Process(
            context,
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // At ~60ms after start, event should not have fired yet
        executed.ShouldBeFalse();

        // Wait another 60ms (total ~120ms from start) - original timer (100ms) should have fired
        await Task.Delay(60);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Sample_MultipleHandlers_IndependentState()
    {
        var handler1 = new SampledEventHandler();
        var handler2 = new SampledEventHandler2();

        var context1 = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = handler1
        };

        var context2 = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = handler2
        };

        var executed1 = false;
        var executed2 = false;

        await this.middleware.Process(
            context1,
            () =>
            {
                executed1 = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        await this.middleware.Process(
            context2,
            () =>
            {
                executed2 = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Neither should be executed immediately
        executed1.ShouldBeFalse();
        executed2.ShouldBeFalse();

        // Wait for sample window to complete
        await Task.Delay(200);

        // Both should be executed independently
        executed1.ShouldBeTrue();
        executed2.ShouldBeTrue();
    }

    [Fact]
    public async Task Sample_ReplacesOldPendingExecution()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        var firstExecuted = false;
        var secondExecuted = false;

        // First event
        await this.middleware.Process(
            context,
            () =>
            {
                firstExecuted = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Small delay
        await Task.Delay(30);

        // Second event before first sample window completes
        await this.middleware.Process(
            context,
            () =>
            {
                secondExecuted = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Wait for sample window to complete
        await Task.Delay(200);

        // First should be replaced, only second should execute
        firstExecuted.ShouldBeFalse();
        secondExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task Sample_EventsAfterWindowExecuteCorrectly()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        var executionCount = 0;

        // First event
        await this.middleware.Process(
            context,
            () =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Wait for sample window to complete
        await Task.Delay(200);
        executionCount.ShouldBe(1);

        // Second event after first sample window completed
        await this.middleware.Process(
            context,
            () =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Wait for second sample window
        await Task.Delay(200);
        executionCount.ShouldBe(2);
    }

    [Fact]
    public async Task Sample_HandlesExceptionsGracefully()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        var secondExecuted = false;

        // First event that throws
        await this.middleware.Process(
            context,
            () => throw new InvalidOperationException("Test exception"),
            CancellationToken.None
        );

        // Wait for sample window to complete (and exception to be logged)
        await Task.Delay(200);

        // Second event should still work
        await this.middleware.Process(
            context,
            () =>
            {
                secondExecuted = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        await Task.Delay(200);
        secondExecuted.ShouldBeTrue();
    }
}

public record SampleTestEvent : IEvent;

public class NoSampleEventHandler : IEventHandler<SampleTestEvent>
{
    public Task Handle(SampleTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class SampledEventHandler : IEventHandler<SampleTestEvent>, IHandlerAttributeMarker
{
    public Task Handle(SampleTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public T? GetAttribute<T>(object message) where T : MediatorMiddlewareAttribute
    {
        if (typeof(T) == typeof(SampleAttribute))
            return new SampleAttribute(100) as T;
        return null;
    }
}

public class SampledEventHandler2 : IEventHandler<SampleTestEvent>, IHandlerAttributeMarker
{
    public Task Handle(SampleTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public T? GetAttribute<T>(object message) where T : MediatorMiddlewareAttribute
    {
        if (typeof(T) == typeof(SampleAttribute))
            return new SampleAttribute(100) as T;
        return null;
    }
}
