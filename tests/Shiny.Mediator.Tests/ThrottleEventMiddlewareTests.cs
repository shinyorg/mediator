using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class ThrottleEventMiddlewareTests
{
    readonly ConfigurationManager config = new();
    readonly ThrottleEventMiddleware<ThrottleTestEvent> middleware;

    public ThrottleEventMiddlewareTests(ITestOutputHelper output)
    {
        var logger = TestHelpers.CreateLogger<ThrottleEventMiddleware<ThrottleTestEvent>>(output);
        this.middleware = new ThrottleEventMiddleware<ThrottleTestEvent>(logger, this.config);
    }

    [Fact]
    public async Task NoThrottle_ExecutesImmediately()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new NoThrottleEventHandler()
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
    public async Task Throttle_DelaysExecution()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
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

        // Wait for throttle delay to pass
        await Task.Delay(200);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Throttle_LastEventWins()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
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
            await Task.Delay(10); // Small delay between events, but less than throttle
        }

        // Wait for throttle delay to complete
        await Task.Delay(200);

        // Only the last event should have been executed
        executionCount.ShouldBe(1);
        lastValue.ShouldBe(5);
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

        // Wait for throttle to complete
        await Task.Delay(200);

        // Both should be executed independently
        executed1.ShouldBeTrue();
        executed2.ShouldBeTrue();
    }

    [Fact]
    public async Task Throttle_ReplacesOldPendingExecution()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
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

        // Second event before first throttle completes
        await this.middleware.Process(
            context,
            () =>
            {
                secondExecuted = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Wait for throttle to complete
        await Task.Delay(200);

        // First should be replaced, only second should execute
        firstExecuted.ShouldBeFalse();
        secondExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task Throttle_EventsAfterDelayExecuteCorrectly()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
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

        // Wait for throttle to complete
        await Task.Delay(200);
        executionCount.ShouldBe(1);

        // Second event after first throttle completed
        await this.middleware.Process(
            context,
            () =>
            {
                executionCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        // Wait for second throttle
        await Task.Delay(200);
        executionCount.ShouldBe(2);
    }

    [Fact]
    public async Task Throttle_HandlesExceptionsGracefully()
    {
        var context = new MockMediatorContext
        {
            Message = new ThrottleTestEvent(),
            MessageHandler = new ThrottledEventHandler()
        };

        var secondExecuted = false;

        // First event that throws
        await this.middleware.Process(
            context,
            () => throw new InvalidOperationException("Test exception"),
            CancellationToken.None
        );

        // Wait for throttle to complete (and exception to be logged)
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