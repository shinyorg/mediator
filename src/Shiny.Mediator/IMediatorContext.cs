using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

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
    
    // /// <summary>
    // /// Publish additional events through this context
    // /// </summary>
    // /// <param name="event"></param>
    // /// <param name="cancellationToken"></param>
    // /// <param name="configure"></param>
    // /// <typeparam name="TEvent"></typeparam>
    // /// <returns></returns>
    // Task Publish<TEvent>(
    //     TEvent @event, 
    //     CancellationToken cancellationToken = default,
    //     Action<IMediatorContext>? configure = null
    // );
    //
    // /// <summary>
    // /// Request additional data through this context
    // /// </summary>
    // /// <param name="request"></param>
    // /// <param name="cancellationToken"></param>
    // /// <param name="configure"></param>
    // /// <typeparam name="TResult"></typeparam>
    // /// <returns></returns>
    // Task<TResult> Request<TResult>(
    //     IRequest<TResult> request, 
    //     CancellationToken cancellationToken = default,
    //     Action<IMediatorContext>? configure = null
    // );
    //
    // /// <summary>
    // /// Send additional commands through this context
    // /// </summary>
    // /// <param name="command"></param>
    // /// <param name="cancellationToken"></param>
    // /// <typeparam name="TCommand"></typeparam>
    // /// <returns></returns>
    // Task Send<TCommand>(
    //     TCommand command, 
    //     CancellationToken cancellationToken = default
    // ) where TCommand : ICommand;
}

public class MediatorContext : IMediatorContext
{
    public MediatorContext(    
        IServiceScope scope,
        object message, 
        ActivitySource activitySource
    )
    {
        this.Message = message;
        this.ServiceScope = scope;
        this.ActivitySource = activitySource;
    }
    
    
    public Guid Id { get; } = Guid.NewGuid();
    public IServiceScope ServiceScope { get; }
    public ActivitySource ActivitySource { get; }
    public object Message { get; }
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
            var newContext = new MediatorContext(this.ServiceScope, msg, this.ActivitySource)
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
}