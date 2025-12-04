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
    /// <param name="newScope">Will create a new service scope from the current one if true</param>
    /// <returns></returns>
    IMediatorContext CreateChild(object? newMessage, bool newScope);

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
    
    
    /// <summary>
    /// Publish an event to the background - this will also start a fresh service scope
    /// </summary>
    /// <param name="event"></param>
    /// <param name="executeInParallel"></param>
    /// <param name="configure"></param>
    /// <typeparam name="TEvent"></typeparam>
    void PublishToBackground<TEvent>(
        TEvent @event, 
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent;
}