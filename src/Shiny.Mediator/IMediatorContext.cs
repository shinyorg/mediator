using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// Per-execution context carried through the mediator pipeline. Holds the originating message, the
/// resolved handler, the service scope, headers, observability state, and links to parent/child contexts.
/// Middleware reads and writes this to share state along the call chain.
/// </summary>
public interface IMediatorContext
{
    /// <summary>
    /// Unique identifier for this execution. Children created via <see cref="CreateChild"/> receive their own ids.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The DI scope used to resolve handlers and middleware for this execution.
    /// </summary>
    IServiceScope ServiceScope { get; }

    /// <summary>
    /// The exception thrown during execution, if any. Set whether or not an exception handler ultimately handled it.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// The <see cref="Activity"/> assigned to this execution for diagnostics/tracing, if one was started.
    /// </summary>
    Activity? Activity { get; }

    /// <summary>
    /// The originating message (command, request, or event) being processed.
    /// </summary>
    object Message { get; }

    /// <summary>
    /// The resolved handler instance for <see cref="Message"/>. Set by the executor before middleware/handler invocation.
    /// </summary>
    object? MessageHandler { get; set; }

    /// <summary>
    /// Read-only view of headers attached to this context.
    /// </summary>
    IReadOnlyDictionary<string, object> Headers { get; }

    /// <summary>
    /// Adds a header value used to share state between middleware and handlers.
    /// </summary>
    void AddHeader(string key, object value);

    /// <summary>
    /// Removes a header by key.
    /// </summary>
    void RemoveHeader(string key);

    /// <summary>
    /// Removes all headers from this context.
    /// </summary>
    void ClearHeaders();

    /// <summary>
    /// The parent context, or <c>null</c> if this is a root context created directly by the mediator.
    /// </summary>
    IMediatorContext? Parent { get; }

    /// <summary>
    /// All child contexts created from this context via <see cref="CreateChild"/>.
    /// </summary>
    IReadOnlyList<IMediatorContext> ChildContexts { get; }

    /// <summary>
    /// UTC timestamp captured when this context was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// When true, registered <see cref="IExceptionHandler"/> instances are skipped for this execution
    /// and exceptions propagate to the caller.
    /// </summary>
    bool BypassExceptionHandlingEnabled { get; set; }

    /// <summary>
    /// When true, no middleware runs for this execution and the handler is invoked directly.
    /// </summary>
    bool BypassMiddlewareEnabled { get; set; }

    /// <summary>
    /// Create a child context with data populated from this parent
    /// </summary>
    /// <param name="newMessage">If you're creating a context for a new message type (ie. Publishing an event from a handler - this would be used)</param>
    /// <param name="reuseScope">If true, reuses the parent service scope. If false, creates a new service scope (caller is responsible for disposal).</param>
    IMediatorContext CreateChild(object? newMessage, bool reuseScope);

    /// <summary>
    /// Starts a child <see cref="Activity"/> on the context's <see cref="Activity"/> for instrumentation.
    /// </summary>
    /// <param name="activityName">Name applied to the child activity.</param>
    Activity? StartActivity(string activityName);

    /// <summary>
    /// Returns the header value for <paramref name="key"/> cast to <typeparamref name="T"/>, or <c>default</c> if missing or of a different type.
    /// </summary>
    T? TryGetValue<T>(string key);


    /// <summary>
    /// Restores a fresh service scope and activity onto this context. Used when an execution is resumed
    /// after the original scope has been disposed (e.g. a scheduled command firing later).
    /// </summary>
    void Rebuild(IServiceScope scope, Activity? activity);


    /// <summary>
    /// Dispatches a request as a child of this context. The child uses a new service scope which is disposed when the call completes.
    /// </summary>
    Task<TResult> Request<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    );


    /// <summary>
    /// Dispatches a command as a child of this context. The child uses a new service scope which is disposed when the call completes.
    /// </summary>
    Task Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand;


    /// <summary>
    /// Publishes an event as a child of this context. The child uses a new service scope which is disposed when the call completes.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="executeInParallel">When true, handlers run concurrently; when false, they run sequentially.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure">Optional callback to configure the child context.</param>
    Task Publish<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent;


    /// <summary>
    /// Fires an event to the background as a child of this context without awaiting handlers.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="executeInParallel">When true, handlers run concurrently; when false, they run sequentially.</param>
    /// <param name="configure">Optional callback to configure the child context.</param>
    void PublishToBackground<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent;
}
