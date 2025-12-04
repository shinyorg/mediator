using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure.Impl;

class MediatorContext(
    IServiceScope scope, 
    object message,
    Activity? activity,
    IMediatorDirector director
) : IMediatorContext
{
    public Guid Id { get; } = Guid.NewGuid();
    public IServiceScope ServiceScope { get; private set; } = scope;
    public Activity? Activity { get; private set; } = activity;
    public object Message => message;
    public object? MessageHandler { get; set; }
    public Exception? Exception { get; set; }
    
    Dictionary<string, object> store = new();
    public IReadOnlyDictionary<string, object> Headers => this.store.ToDictionary();
    public void AddHeader(string key, object value) => this.store.Add(key, value);
    public void RemoveHeader(string key) => this.store.Remove(key);
    public void ClearHeaders() => this.store.Clear();

    public bool BypassExceptionHandlingEnabled { get; set; }
    public bool BypassMiddlewareEnabled { get; set; }
    
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    
    public IMediatorContext? Parent { get; private init; }


    readonly List<IMediatorContext> children = new();

    public IReadOnlyList<IMediatorContext> ChildContexts
    {
        get
        {
            lock (this.children)
                return this.children;
        }
    }

    
    public IMediatorContext CreateChild(object? newMessage, bool newScope)
    {
        lock (this.children)
        {
            var msg = newMessage ?? this.Message;
            var act = this.StartActivity("child_mediator");

            var scope = newScope
                ? this.ServiceScope
                : this.ServiceScope.ServiceProvider.CreateScope();
            
            var newContext = new MediatorContext(
                scope, 
                msg,
                act,
                director
            )
            {
                Parent = this,
                BypassExceptionHandlingEnabled = this.BypassExceptionHandlingEnabled,
                BypassMiddlewareEnabled = this.BypassMiddlewareEnabled
                // store = this.store.ToDictionary() // DO NOT pass headers down to child contexts - crashes cache
            };
            this.children.Add(newContext);
            return newContext;
        }
    }
    

    public Activity? StartActivity(string activityName)
    {
        var childActivity = this.Activity?.Start();
        
        if (childActivity != null)
        {
            childActivity.SetTag("operation_id", this.Id);
            foreach (var header in this.Headers)
                childActivity.SetTag(header.Key, header.Value);
        }
        return childActivity;
    }
    
    
    public T? TryGetValue<T>(string key)
    {
        if (this.Headers.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }

    public void Rebuild(IServiceScope scope, Activity? activity)
    {
        this.ServiceScope = scope;
        this.Activity = activity;
    }


    public Task<TResult> Request<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        var newContext = this.CreateChild(request, false);
        configure?.Invoke(newContext);
        return director
            .GetRequestExecutor(request)
            .Request(newContext, request, cancellationToken);
    }

    
    public Task Send<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default, 
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand
    {
        var newContext = this.CreateChild(command, false);
        configure?.Invoke(newContext);
        return director
            .GetCommandExecutor(command)
            .Send(newContext, command, cancellationToken);
    }
    
    
    public Task Publish<TEvent>(
        TEvent @event, 
        bool executeInParallel = true,
        CancellationToken cancellationToken = default, 
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        var newContext = this.CreateChild(@event, false);
        configure?.Invoke(newContext);
        return director
            .GetEventExecutor(@event)
            .Publish(newContext, @event, executeInParallel, cancellationToken);
    }


    public void PublishToBackground<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        var newContext = this.CreateChild(@event, true);
        configure?.Invoke(newContext);
        director
            .GetEventExecutor(@event)
            .PublishToBackground(newContext, @event, executeInParallel);
    }
}