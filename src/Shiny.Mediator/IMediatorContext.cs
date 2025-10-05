using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public interface IMediatorContext
{
    /// <summary>
    /// Id of request train
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Current service scope
    /// </summary>
    IServiceScope ServiceScope { get; }
    
    /// <summary>
    /// If an exception was thrown during the request, this will be populated
    /// It does not mean the exception was handled, just that it was thrown
    /// </summary>
    Exception? Exception { get; }
    
    /// <summary>
    /// Assigned activity source for observability
    /// </summary>
    Activity? Activity { get; }
    
    /// <summary>
    /// Message
    /// </summary>
    object Message { get; }
    
    /// <summary>
    /// Message Handler
    /// </summary>
    object? MessageHandler { get; set; }

    /// <summary>
    /// Readonly headers
    /// </summary>
    IReadOnlyDictionary<string, object> Headers { get; }
    
    /// <summary>
    /// Add Header
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void AddHeader(string key, object value);
    
    /// <summary>
    /// Remove Header by key
    /// </summary>
    /// <param name="key"></param>
    void RemoveHeader(string key);
    
    /// <summary>
    /// Clear headers
    /// </summary>
    void ClearHeaders();
    
    /// <summary>
    /// The parent of this context
    /// </summary>
    IMediatorContext? Parent { get; }
    
    /// <summary>
    /// All child contexts under this parent
    /// </summary>
    IReadOnlyList<IMediatorContext> ChildContexts { get; }

    /// <summary>
    /// The timestamp of when this context was created
    /// </summary>
    DateTimeOffset CreatedAt { get; }
    
    /// <summary>
    /// Allows you to bypass all exception handlers for this call
    /// </summary>
    bool BypassExceptionHandlingEnabled { get; set; }
    
    /// <summary>
    /// Allows you to disable allow middleware for this call
    /// </summary>
    bool BypassMiddlewareEnabled { get; set; }
    
    /// <summary>
    /// Create a child context with data populated from this parent
    /// </summary>
    /// <param name="newMessage">If you're creating a context for a new message type (ie. Publishing an event from a handler - this would be used)</param>
    /// <returns></returns>
    IMediatorContext CreateChild(object? newMessage);

    /// <summary>
    /// Start an instrumentation activity
    /// </summary>
    /// <param name="activityName"></param>
    /// <returns></returns>
    Activity? StartActivity(string activityName);
    
    /// <summary>
    /// Try to get value from mediator headers
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? TryGetValue<T>(string key);


    /// <summary>
    /// This is meant to rebuild a context if the service scope has died (ie. deferred command)
    /// </summary>
    /// 
    /// <param name="scope"></param>
    /// <param name="activity"></param>
    void Rebuild(IServiceScope scope, Activity? activity);
    
    
    /// <summary>
    /// Send a request in the same scope
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    Task<TResult> Request<TResult>(
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default, 
        Action<IMediatorContext>? configure = null
    );
    
    
    /// <summary>
    /// Send a command in the same scope
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    Task Send<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand;
    
    
    /// <summary>
    /// Publish a command within the same scope
    /// </summary>
    /// <param name="event"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task Publish<TEvent>(
        TEvent @event, 
        bool executeInParallel = true,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent;
}

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

    
    public IMediatorContext CreateChild(object? newMessage)
    {
        lock (this.children)
        {
            var msg = newMessage ?? this.Message;
            var act = this.StartActivity("child_mediator");
            
            var newContext = new MediatorContext(
                ServiceScope, 
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
        var newContext = this.CreateChild(request);
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
        var newContext = this.CreateChild(command);
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
        var newContext = this.CreateChild(@event);
        configure?.Invoke(newContext);
        return director
            .GetEventExecutor(@event)
            .Publish(newContext, @event, executeInParallel, cancellationToken);
    }
}