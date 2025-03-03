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
    /// Assigned activity source for observability
    /// </summary>
    ActivitySource ActivitySource { get; }
    
    //IMediator Mediator { get; }
    
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
    IRequestExecutor requestExecutor,
    ICommandExecutor commandExecutor,
    IEventExecutor eventExecutor
) : IMediatorContext
{
    static readonly ActivitySource activitySource = new("Shiny.Mediator");
    
    public Guid Id { get; } = Guid.NewGuid();
    public IServiceScope ServiceScope => scope;
    public ActivitySource ActivitySource => activitySource;
    public object Message => message;
    public object? MessageHandler { get; set; }
    
    Dictionary<string, object> store = new();
    public IReadOnlyDictionary<string, object> Headers => this.store.ToDictionary();
    public void AddHeader(string key, object value) => this.store.Add(key, value);
    
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
            var newContext = new MediatorContext(
                ServiceScope, 
                msg,
                requestExecutor, 
                commandExecutor,
                eventExecutor
            )
            {
                Parent = this,
                BypassExceptionHandlingEnabled = this.BypassExceptionHandlingEnabled,
                BypassMiddlewareEnabled = this.BypassMiddlewareEnabled,
                store = this.store.ToDictionary() // copy over
            };
            this.children.Add(newContext);
            return newContext;
        }
    }
    

    public Activity? StartActivity(string activityName)
    {
        var activity = this.ActivitySource?.StartActivity(activityName);
        if (activity != null)
        {
            activity.SetTag("operation_id", this.Id);
            foreach (var header in this.Headers)
                activity.SetTag(header.Key, header.Value);
        }
        return activity;
    }
    
    
    public T? TryGetValue<T>(string key)
    {
        if (this.Headers.TryGetValue(key, out var value) && value is T t)
            return t;

        return default;
    }


    public Task<TResult> Request<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        var newContext = this.CreateChild(request);
        configure?.Invoke(newContext);
        return requestExecutor.Request(newContext, request, cancellationToken);
    }

    
    public Task Send<TCommand>(
        TCommand command, 
        CancellationToken cancellationToken = default, 
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand
    {
        var newContext = this.CreateChild(command);
        configure?.Invoke(newContext);
        return commandExecutor.Send(newContext, command, cancellationToken);
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
        return eventExecutor.Publish(newContext, @event, executeInParallel, cancellationToken);
    }
}