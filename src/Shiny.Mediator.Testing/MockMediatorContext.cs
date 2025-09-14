using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Testing;


public class MockMediatorContext(object message) : IMediatorContext
{
    public Guid Id { get; } = Guid.NewGuid();
    public IServiceScope ServiceScope { get; set; }
    public Activity? Activity { get; set; }
    public object Message => message;
    public object? MessageHandler { get; set; }

    readonly Dictionary<string, object> settableHeaders = new();
    public IReadOnlyDictionary<string, object> Headers => this.settableHeaders;
    public void AddHeader(string key, object value) => this.settableHeaders.Add(key, value);
    public void RemoveHeader(string key) => this.settableHeaders.Remove(key);
    public void ClearHeaders() => this.settableHeaders.Clear();

    public Exception? Exception { get; set; }
    public IMediatorContext? Parent { get; }
    public IReadOnlyList<IMediatorContext> ChildContexts { get; }
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public bool BypassExceptionHandlingEnabled { get; set; }
    public bool BypassMiddlewareEnabled { get; set; }
    public IMediatorContext CreateChild(object? newMessage)
    {
        throw new NotImplementedException();
    }

    public Activity? StartActivity(string activityName) => null;

    public T? TryGetValue<T>(string key)
    {
        throw new NotImplementedException();
    }

    public void Rebuild(IServiceScope scope, Activity? activity)
    {
        this.Activity = activity;
        this.ServiceScope = scope;
    }

    public Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
    {
        throw new NotImplementedException();
    }

    public Task Send<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand => Task.CompletedTask;

    public Task Publish<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent => Task.CompletedTask;
}