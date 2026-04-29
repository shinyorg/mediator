using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class QueuedEventMiddlewareTests
{
    readonly ConfigurationManager config = new();
    readonly QueuedEventMiddleware<SampleTestEvent> sampleMiddleware;
    readonly QueuedEventMiddleware<ThrottleTestEvent> throttleMiddleware;

    public QueuedEventMiddlewareTests(ITestOutputHelper output)
    {
        var sampleLogger = TestHelpers.CreateLogger<QueuedEventMiddleware<SampleTestEvent>>(output);
        this.sampleMiddleware = new QueuedEventMiddleware<SampleTestEvent>(sampleLogger, this.config);

        var throttleLogger = TestHelpers.CreateLogger<QueuedEventMiddleware<ThrottleTestEvent>>(output);
        this.throttleMiddleware = new QueuedEventMiddleware<ThrottleTestEvent>(throttleLogger, this.config);
    }

    // ── Sample tests ────────────────────────────────────────────────────

    [Fact]
    public async Task NoSample_ExecutesImmediately()
    {
        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new NoSampleEventHandler()
        };

        var executed = false;
        await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
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
            await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
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

        await this.sampleMiddleware.Process(
            context1,
            () =>
            {
                executed1 = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
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
        await this.sampleMiddleware.Process(
            context,
            () => throw new InvalidOperationException("Test exception"),
            CancellationToken.None
        );

        // Wait for sample window to complete (and exception to be logged)
        await Task.Delay(200);

        // Second event should still work
        await this.sampleMiddleware.Process(
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

    // ── Throttle tests ──────────────────────────────────────────────────

    [Fact]
    public async Task NoThrottle_ExecutesImmediately()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new NoThrottleEventHandler()
        };

        var executed = false;
        await this.throttleMiddleware.Process(
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
    public async Task Throttle_FirstEventExecutesImmediately()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
        };

        var executed = false;
        await this.throttleMiddleware.Process(
            context,
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // First event should execute immediately
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Throttle_SecondEventWithinWindowIsIgnored()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
        };

        var executionCount = 0;

        // First event - should execute immediately
        await this.throttleMiddleware.Process(
            context,
            () =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );
        executionCount.ShouldBe(1);

        // Second event within cooldown window - should be discarded
        await this.throttleMiddleware.Process(
            context,
            () =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );
        executionCount.ShouldBe(1);
    }

    [Fact]
    public async Task Throttle_AfterCooldown_NextEventExecutesImmediately()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
        };

        var executionCount = 0;

        // First event - executes immediately
        await this.throttleMiddleware.Process(
            context,
            () =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );
        executionCount.ShouldBe(1);

        // Wait for cooldown to expire
        await Task.Delay(200);

        // Next event after cooldown - should execute immediately
        await this.throttleMiddleware.Process(
            context,
            () =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );
        executionCount.ShouldBe(2);
    }

    [Fact]
    public async Task Throttle_MultipleHandlers_IndependentState()
    {
        var handler1 = new ThrottledEventHandler();
        var handler2 = new ThrottledEventHandler2();

        var context1 = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = handler1
        };

        var context2 = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = handler2
        };

        var executed1 = false;
        var executed2 = false;

        // Both first events should execute immediately
        await this.throttleMiddleware.Process(
            context1,
            () =>
            {
                executed1 = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        await this.throttleMiddleware.Process(
            context2,
            () =>
            {
                executed2 = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        executed1.ShouldBeTrue();
        executed2.ShouldBeTrue();
    }

    [Fact]
    public async Task Throttle_ExceptionDoesNotBreakSubsequentWindows()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
        };

        // First event throws - but it still executes (throttle passes through)
        var threw = false;
        try
        {
            await this.throttleMiddleware.Process(
                context,
                () => throw new InvalidOperationException("Test exception"),
                CancellationToken.None
            );
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }
        threw.ShouldBeTrue();

        // Wait for cooldown to expire
        await Task.Delay(200);

        // Next event should still work
        var executed = false;
        await this.throttleMiddleware.Process(
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
    public async Task Throttle_RapidFire_OnlyFirstExecutes()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
        };

        var executionCount = 0;

        // Fire 5 events rapidly
        for (var i = 0; i < 5; i++)
        {
            await this.throttleMiddleware.Process(
                context,
                () =>
                {
                    executionCount++;
                    return Task.CompletedTask;
                },
                CancellationToken.None
            );
        }

        // Only the first should have executed
        executionCount.ShouldBe(1);
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

public record ThrottleTestEvent : IEvent;

public class NoThrottleEventHandler : IEventHandler<ThrottleTestEvent>
{
    public Task Handle(ThrottleTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class ThrottledEventHandler : IEventHandler<ThrottleTestEvent>, IHandlerAttributeMarker
{
    public Task Handle(ThrottleTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public T? GetAttribute<T>(object message) where T : MediatorMiddlewareAttribute
    {
        if (typeof(T) == typeof(ThrottleAttribute))
            return new ThrottleAttribute(100) as T;
        return null;
    }
}

public class ThrottledEventHandler2 : IEventHandler<ThrottleTestEvent>, IHandlerAttributeMarker
{
    public Task Handle(ThrottleTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public T? GetAttribute<T>(object message) where T : MediatorMiddlewareAttribute
    {
        if (typeof(T) == typeof(ThrottleAttribute))
            return new ThrottleAttribute(100) as T;
        return null;
    }
}
