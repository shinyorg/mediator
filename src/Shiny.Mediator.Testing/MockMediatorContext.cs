using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Testing;


public class MockMediatorContext(object message) : IMediatorContext
{
    public virtual Guid Id { get; } = Guid.NewGuid();
    public virtual IServiceScope ServiceScope { get; set; }
    public virtual Activity? Activity { get; set; }
    public virtual object Message => message;
    public virtual object? MessageHandler { get; set; }

    readonly Dictionary<string, object> settableHeaders = new();
    public virtual IReadOnlyDictionary<string, object> Headers => this.settableHeaders;
    public virtual void AddHeader(string key, object value) => this.settableHeaders.Add(key, value);
    public virtual void RemoveHeader(string key) => this.settableHeaders.Remove(key);
    public virtual void ClearHeaders() => this.settableHeaders.Clear();

    public virtual Exception? Exception { get; set; }
    public virtual IMediatorContext? Parent { get; }
    public virtual IReadOnlyList<IMediatorContext> ChildContexts { get; }
    public virtual DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public virtual bool BypassExceptionHandlingEnabled { get; set; }
    public virtual bool BypassMiddlewareEnabled { get; set; }
    public virtual IMediatorContext CreateChild(object? newMessage, bool newScope)
    {
        return this;
    }

    public virtual Activity? StartActivity(string activityName) => null;

    public virtual T? TryGetValue<T>(string key)
    {
        return default;
    }

    public virtual void Rebuild(IServiceScope scope, Activity? activity)
    {
        this.Activity = activity;
        this.ServiceScope = scope;
    }

    public virtual Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
    {
        throw new NotImplementedException();
    }

    public virtual Task Send<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand => Task.CompletedTask;

    public virtual Task Publish<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent => Task.CompletedTask;

    public virtual void PublishToBackground<TEvent>(TEvent @event, bool executeInParallel = true, Action<IMediatorContext>? configure = null) where TEvent : IEvent
    {
        
    }
}