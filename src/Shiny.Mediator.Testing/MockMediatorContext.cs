using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Testing;


/// <summary>
/// In-memory <see cref="IMediatorContext"/> used by <see cref="MockMediator"/> and unit tests.
/// All members are virtual so individual properties or behaviors can be overridden when verifying middleware
/// or handlers that interact with the context (headers, activity, child contexts, nested calls).
/// </summary>
/// <param name="message">The message (command, request, or event) associated with this context.</param>
public class MockMediatorContext(object message) : IMediatorContext
{
    /// <inheritdoc/>
    public virtual Guid Id { get; } = Guid.NewGuid();
    /// <inheritdoc/>
    public virtual IServiceScope ServiceScope { get; set; }
    /// <inheritdoc/>
    public virtual Activity? Activity { get; set; }
    /// <inheritdoc/>
    public virtual object Message => message;
    /// <inheritdoc/>
    public virtual object? MessageHandler { get; set; }

    readonly Dictionary<string, object> settableHeaders = new();
    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, object> Headers => this.settableHeaders;
    /// <inheritdoc/>
    public virtual void AddHeader(string key, object value) => this.settableHeaders.Add(key, value);
    /// <inheritdoc/>
    public virtual void RemoveHeader(string key) => this.settableHeaders.Remove(key);
    /// <inheritdoc/>
    public virtual void ClearHeaders() => this.settableHeaders.Clear();

    /// <inheritdoc/>
    public virtual Exception? Exception { get; set; }
    /// <inheritdoc/>
    public virtual IMediatorContext? Parent { get; }
    /// <inheritdoc/>
    public virtual IReadOnlyList<IMediatorContext> ChildContexts { get; }
    /// <inheritdoc/>
    public virtual DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    /// <inheritdoc/>
    public virtual bool BypassExceptionHandlingEnabled { get; set; }
    /// <inheritdoc/>
    public virtual bool BypassMiddlewareEnabled { get; set; }
    /// <inheritdoc/>
    public virtual IMediatorContext CreateChild(object? newMessage, bool reuseScope)
    {
        return this;
    }

    /// <inheritdoc/>
    public virtual Activity? StartActivity(string activityName) => null;

    /// <inheritdoc/>
    public virtual T? TryGetValue<T>(string key)
    {
        return default;
    }

    /// <inheritdoc/>
    public virtual void Rebuild(IServiceScope scope, Activity? activity)
    {
        this.Activity = activity;
        this.ServiceScope = scope;
    }

    /// <inheritdoc/>
    public virtual Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual Task Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual Task Publish<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual void PublishToBackground<TEvent>(TEvent @event, bool executeInParallel = true, Action<IMediatorContext>? configure = null) where TEvent : IEvent
    {

    }
}