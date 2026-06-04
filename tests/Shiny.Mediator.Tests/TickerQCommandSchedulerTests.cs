using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Tests.Mocks;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models;
using TickerQ.Utilities.Base;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class TickerQCommandSchedulerTests(ITestOutputHelper output)
{
    readonly SysTextJsonSerializerService serializer = new();


    [Fact]
    public async Task Schedule_Persists_RoundTrippable_Payload()
    {
        var manager = new FakeTimeTickerManager();
        var scheduler = new TickerQCommandScheduler(
            manager,
            this.serializer,
            TestHelpers.CreateLogger<TickerQCommandScheduler>(output)
        );

        var command = new TickerQTestCommand { Name = "abc", Value = 42 };
        var context = new MockMediatorContext { Message = command };
        var dueAt = DateTimeOffset.UtcNow.AddMinutes(5);

        await scheduler.Schedule(context, dueAt, (_, _) => Task.CompletedTask, CancellationToken.None);

        manager.Added.ShouldNotBeNull();
        manager.Added!.Function.ShouldBe(ScheduledCommandRunner.FunctionName);
        manager.Added.ExecutionTime.ShouldBe(dueAt.UtcDateTime);

        var payload = TickerHelper.ReadTickerRequest<ScheduledCommandPayload>(manager.Added.Request);
        payload.ShouldNotBeNull();
        Type.GetType(payload.CommandType).ShouldBe(typeof(TickerQTestCommand));

        // the command body must round-trip with its data intact - proves runtime-type serialization, not "{}"
        var roundTripped = (TickerQTestCommand)this.serializer.Deserialize(payload.CommandJson, typeof(TickerQTestCommand));
        roundTripped.Name.ShouldBe("abc");
        roundTripped.Value.ShouldBe(42);
    }


    [Fact]
    public async Task Runner_Rehydrates_And_Dispatches_With_Bypass()
    {
        var command = new TickerQTestCommand { Name = "xyz", Value = 7 };
        var payload = new ScheduledCommandPayload
        {
            CommandType = typeof(TickerQTestCommand).AssemblyQualifiedName!,
            CommandJson = this.serializer.Serialize(command)
        };

        var mediator = new FakeMediator();
        var runner = new ScheduledCommandRunner(
            mediator,
            this.serializer,
            TestHelpers.CreateLogger<ScheduledCommandRunner>(output)
        );
        var context = new TickerFunctionContext<ScheduledCommandPayload>(new TickerFunctionContext(), payload);

        await runner.Execute(context, CancellationToken.None);

        var sent = mediator.SentCommand.ShouldBeOfType<TickerQTestCommand>();
        sent.Name.ShouldBe("xyz");
        sent.Value.ShouldBe(7);
        mediator.BypassMiddleware.ShouldBeTrue();
        mediator.BypassExceptionHandling.ShouldBeTrue();
    }
}


class TickerQTestCommand : ICommand
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}


file class FakeTimeTickerManager : ITimeTickerManager<TimeTickerEntity>
{
    public TimeTickerEntity? Added { get; private set; }

    public Task<TickerResult<TimeTickerEntity>> AddAsync(TimeTickerEntity entity, CancellationToken cancellationToken = default)
    {
        this.Added = entity;
        return Task.FromResult<TickerResult<TimeTickerEntity>>(null!);
    }

    public Task<TickerResult<TimeTickerEntity>> UpdateAsync(TimeTickerEntity timeTicker, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TickerResult<TimeTickerEntity>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TickerResult<List<TimeTickerEntity>>> AddBatchAsync(List<TimeTickerEntity> entities, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TickerResult<List<TimeTickerEntity>>> UpdateBatchAsync(List<TimeTickerEntity> timeTickers, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TickerResult<TimeTickerEntity>> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<TickerResult<TimeTickerEntity>> AddAsync<TFunction>(DateTime? executionTime = null, CancellationToken cancellationToken = default)
        where TFunction : class, ITickerFunction
        => throw new NotImplementedException();

    public Task<TickerResult<TimeTickerEntity>> AddAsync<TFunction, TRequest>(DateTime? executionTime, TRequest request, CancellationToken cancellationToken = default)
        where TFunction : class, ITickerFunction<TRequest>
        => throw new NotImplementedException();
}


file class FakeMediator : IMediator
{
    public ICommand? SentCommand { get; private set; }
    public bool BypassMiddleware { get; private set; }
    public bool BypassExceptionHandling { get; private set; }

    public Task<IMediatorContext> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
        where TCommand : ICommand
    {
        this.SentCommand = request;
        var context = new MockMediatorContext { Message = request! };
        configure?.Invoke(context);
        this.BypassMiddleware = context.BypassMiddlewareEnabled;
        this.BypassExceptionHandling = context.BypassExceptionHandlingEnabled;
        return Task.FromResult<IMediatorContext>(context);
    }

    public Task<(IMediatorContext Context, TResult Result)> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
        => throw new NotImplementedException();

    public IAsyncEnumerable<(IMediatorContext Context, TResult Result)> Request<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
        => throw new NotImplementedException();

    public Task<IMediatorContext> Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default, bool executeInParallel = true, Action<IMediatorContext>? configure = null)
        where TEvent : IEvent
        => throw new NotImplementedException();

    public void PublishToBackground<TEvent>(TEvent @event, bool executeInParallel = true, Action<IMediatorContext>? configure = null)
        where TEvent : IEvent
        => throw new NotImplementedException();

    public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action)
        where TEvent : IEvent
        => throw new NotImplementedException();
}
