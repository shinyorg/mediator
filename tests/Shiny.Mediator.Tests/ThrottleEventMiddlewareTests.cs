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
    public async Task Throttle_FirstEventExecutesImmediately()
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
        await this.middleware.Process(
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
        await this.middleware.Process(
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
        await this.middleware.Process(
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
        await this.middleware.Process(
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
            await this.middleware.Process(
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
            await this.middleware.Process(
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
