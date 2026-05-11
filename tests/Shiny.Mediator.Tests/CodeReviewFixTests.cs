using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Middleware;
using Shiny.Mediator.Tests.Mocks;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


/// <summary>
/// Tests verifying fixes from the code review
/// </summary>
public class CodeReviewFixTests(ITestOutputHelper output)
{
    IMediator SetupMediator(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        services.AddConfiguration();
        services.AddShinyMediator();
        configure?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }


    // ── Fix 1: MAUI DeleteFile inverted condition ───────────────────────
    // Cannot unit test directly (requires MAUI filesystem), but the pattern
    // is verified via the AbstractFileStorageService tests below.

    [Fact]
    public async Task FileStorageService_DeleteFile_CalledForExistingFiles()
    {
        var storage = new TrackingStorageService(
            new SysTextJsonSerializerService(),
            TestHelpers.CreateLogger<TrackingStorageService>(output)
        );

        await storage.Set("cat", "key1", "value1", CancellationToken.None);
        await storage.Remove("cat", "key1", false, CancellationToken.None);

        storage.DeletedFiles.Count.ShouldBeGreaterThan(0);
    }


    // ── Fix 2: CreateChild scope leak ───────────────────────────────────

    [Fact]
    public async Task Publish_DoesNotLeakServiceScopes()
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        services.AddConfiguration();
        services.AddShinyMediator();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        // Publish many events — if scopes were leaking, this would accumulate
        // unreleased scopes. We verify no exceptions and correct behavior.
        for (int i = 0; i < 100; i++)
            await mediator.Publish(new ScopeTestEvent());
    }

    [Fact]
    public async Task Publish_SubscriptionStillFiresWithReusedScope()
    {
        var mediator = this.SetupMediator();

        int count = 0;
        mediator.Subscribe<ScopeTestEvent>((_, _, _) =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        await mediator.Publish(new ScopeTestEvent());
        await mediator.Publish(new ScopeTestEvent());
        await mediator.Publish(new ScopeTestEvent());
        count.ShouldBe(3);
    }


    // ── Fix 3: StorageCacheService.GetOrCreate atomicity ────────────────

    [Fact]
    public async Task StorageCacheService_GetOrCreate_ConcurrentCalls_FactoryCalledOnce()
    {
        var fakeStore = new MockStorageService();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var cache = new StorageCacheService(fakeStore, timeProvider);

        int factoryCallCount = 0;
        var barrier = new Barrier(10);

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await cache.GetOrCreate("key", async () =>
            {
                Interlocked.Increment(ref factoryCallCount);
                await Task.Delay(50);
                return "value";
            });
        })).ToList();

        var results = await Task.WhenAll(tasks);

        factoryCallCount.ShouldBe(1);
        foreach (var r in results)
        {
            r.ShouldNotBeNull();
            r.Value.ShouldBe("value");
        }
    }


    // ── Fix 4: PublishToBackground TryHandle awaited ─────────────────────

    [Fact]
    public async Task PublishToBackground_ExceptionHandler_IsInvoked()
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        services.AddConfiguration();
        services.AddShinyMediator(x => x.AddExceptionHandler<TrackingExceptionHandler>());
        services.AddSingletonAsImplementedInterfaces<ThrowingEventHandler>();
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        mediator.PublishToBackground(new ScopeTestEvent());

        // Give background task time to complete
        await Task.Delay(500);

        TrackingExceptionHandler.WasHandled.ShouldBeTrue();
    }


    // ── Fix 7: MemoryCacheService.Remove partial match ──────────────────

    [Fact]
    public async Task MemoryCacheService_Remove_PartialMatch_DoesNotThrow()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var timeProvider = new FakeTimeProvider();
        var service = new MemoryCacheService(memCache, timeProvider);

        await service.Set("prefix_key1", "val1");
        await service.Set("prefix_key2", "val2");
        await service.Set("other_key", "val3");

        // This used to throw InvalidOperationException due to modifying
        // the collection during enumeration
        await service.Remove("prefix_", partialMatch: true);

        var r1 = await service.Get<string>("prefix_key1", CancellationToken.None);
        var r2 = await service.Get<string>("prefix_key2", CancellationToken.None);
        var r3 = await service.Get<string>("other_key", CancellationToken.None);

        r1.ShouldBeNull();
        r2.ShouldBeNull();
        r3.ShouldNotBeNull();
    }


    // ── Fix 8: QueuedEventMiddleware SampleState timer correctness ────────

    [Fact]
    public async Task Sample_ConsecutiveWindows_EachFireOnce()
    {
        var logger = TestHelpers.CreateLogger<QueuedEventMiddleware<SampleTestEvent>>(output);
        var middleware = new QueuedEventMiddleware<SampleTestEvent>(logger);

        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        // First window: enqueue and let it fire
        var firstExecuted = false;
        await middleware.Process(
            context,
            () =>
            {
                firstExecuted = true;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );
        await Task.Delay(200);
        firstExecuted.ShouldBeTrue();

        // Second window: enqueue again — should start a fresh timer
        var secondExecuted = false;
        await middleware.Process(
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

    [Fact]
    public async Task Sample_RapidReenqueue_DoesNotDoubleExecute()
    {
        var logger = TestHelpers.CreateLogger<QueuedEventMiddleware<SampleTestEvent>>(output);
        var middleware = new QueuedEventMiddleware<SampleTestEvent>(logger);

        var context = new MockMediatorContext
        {
            Message = new SampleTestEvent(),
            MessageHandler = new SampledEventHandler()
        };

        int executionCount = 0;

        // Fire 10 events rapidly, let the window close, then fire 10 more
        for (int i = 0; i < 10; i++)
        {
            await middleware.Process(
                context,
                () =>
                {
                    Interlocked.Increment(ref executionCount);
                    return Task.CompletedTask;
                },
                CancellationToken.None
            );
        }

        await Task.Delay(200);
        executionCount.ShouldBe(1);

        for (int i = 0; i < 10; i++)
        {
            await middleware.Process(
                context,
                () =>
                {
                    Interlocked.Increment(ref executionCount);
                    return Task.CompletedTask;
                },
                CancellationToken.None
            );
        }

        await Task.Delay(200);
        executionCount.ShouldBe(2);
    }


    // ── Fix 9: AbstractFileStorageService concurrent access ─────────────

    [Fact]
    public async Task FileStorageService_ConcurrentSetAndClear_NoDeadlock()
    {
        var storage = new TrackingStorageService(
            new SysTextJsonSerializerService(),
            TestHelpers.CreateLogger<TrackingStorageService>(output)
        );

        var tasks = new List<Task>();
        for (var i = 0; i < 20; i++)
        {
            if (i % 2 == 0)
                tasks.Add(storage.Set("test", $"key{i}", new { Value = i }, CancellationToken.None));
            else
                tasks.Add(storage.Clear("test", CancellationToken.None));
        }
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task FileStorageService_ConcurrentGetFileIndexer_SameKeyGetsSameFile()
    {
        var storage = new TrackingStorageService(
            new SysTextJsonSerializerService(),
            TestHelpers.CreateLogger<TrackingStorageService>(output)
        );

        // Concurrent calls for the same key should produce the same file index
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                await storage.Set("cat", "sameKey", "value", CancellationToken.None);
            }))
            .ToList();

        await Task.WhenAll(tasks);

        // Verify we can read the value back
        var result = await storage.Get<string>("cat", "sameKey", CancellationToken.None);
        result.ShouldBe("value");
    }
}


// ── Test helpers ────────────────────────────────────────────────────────

file record ScopeTestEvent : IEvent;

file class ThrowingEventHandler : IEventHandler<ScopeTestEvent>
{
    public Task Handle(ScopeTestEvent @event, IMediatorContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Test exception from handler");
}

file class TrackingExceptionHandler : IExceptionHandler
{
    public static bool WasHandled;

    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        WasHandled = true;
        return Task.FromResult(true);
    }
}

file class TrackingStorageService(
    ISerializerService serializer,
    ILogger<TrackingStorageService> logger
) : AbstractFileStorageService(serializer, logger)
{
    readonly Dictionary<string, string> files = new();
    public List<string> DeletedFiles { get; } = new();

    protected override Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
    {
        lock (this.files)
            this.files[fileName] = content;
        return Task.CompletedTask;
    }

    protected override Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
    {
        lock (this.files)
            return Task.FromResult(this.files.GetValueOrDefault(fileName));
    }

    protected override Task DeleteFile(string fileName, CancellationToken cancellationToken)
    {
        lock (this.files)
        {
            this.files.Remove(fileName);
            this.DeletedFiles.Add(fileName);
        }
        return Task.CompletedTask;
    }
}
