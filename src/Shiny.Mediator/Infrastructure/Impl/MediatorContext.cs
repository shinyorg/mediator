using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            // snapshot - the caller enumerates outside this lock, and parallel event publishing
            // adds children concurrently, so handing out the live list throws mid-enumeration
            lock (this.children)
                return this.children.ToList();
        }
    }

    
    public IMediatorContext CreateChild(object? newMessage, bool reuseScope)
    {
        lock (this.children)
        {
            var msg = newMessage ?? this.Message;
            var act = this.StartActivity("child_mediator");

            var scope = reuseScope
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


    public async Task<TResult> Request<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        var newContext = this.CreateChild(request, false);
        configure?.Invoke(newContext);
        try
        {
            return await director
                .GetRequestExecutor(request)
                .Request(newContext, request, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            newContext.ServiceScope.Dispose();
        }
    }


    public async Task Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand
    {
        var newContext = this.CreateChild(command, false);
        configure?.Invoke(newContext);
        try
        {
            await director
                .GetCommandExecutor(command)
                .Send(newContext, command, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            newContext.ServiceScope.Dispose();
        }
    }


    public async Task Publish<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        var newContext = this.CreateChild(@event, false);
        configure?.Invoke(newContext);
        try
        {
            await director
                .GetEventExecutor(@event)
                .Publish(newContext, @event, executeInParallel, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            newContext.ServiceScope.Dispose();
        }
    }


    public void PublishToBackground<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        // this work outlives the current dispatch, so it cannot share the caller's scope - that scope is
        // disposed the moment the outer Request/Send/Publish returns (and, under ASP.NET, when the request
        // ends), leaving handlers resolving against a dead IServiceScope. We own this scope and dispose it
        // once the handlers finish. Mirrors MediatorImpl.PublishToBackground.
        var newContext = (MediatorContext)this.CreateChild(@event, false);
        configure?.Invoke(newContext);

        var logger = newContext
            .ServiceScope
            .ServiceProvider
            .GetService<ILogger<TEvent>>();

        _ = Task.Run(async () =>
        {
            try
            {
                await director
                    .GetEventExecutor(@event)
                    .Publish(newContext, @event, executeInParallel, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // previously swallowed - a background publish reported nothing, while the same call on
                // IMediator ran the exception handler chain
                await MediatorExceptionHandling
                    .TryHandle(newContext, ex, logger)
                    .ConfigureAwait(false);
            }
            finally
            {
                newContext.ServiceScope.Dispose();
            }
        });
    }
}