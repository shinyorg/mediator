namespace Shiny.Mediator;


/// <summary>
/// A void command to be handled by a mediator handler
/// </summary>
public interface ICommand;

/// <summary>
/// A marker interface for scheduled commands
/// </summary>
public interface IScheduledCommand : ICommand
{
    /// <summary>
    /// The due date for the command
    /// </summary>
    DateTimeOffset DueAt { get; set; }
}


/// <summary>
/// Publish an event to mediator
/// </summary>
public interface IEvent;

/// <summary>
/// Marks a contract that requests an asynchronous stream of values from a handler.
/// Dispatched through the mediator's stream request pipeline and consumed as an <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <typeparam name="TResult">The element type produced by the stream.</typeparam>
public interface IStreamRequest<out TResult>;

/// <summary>
/// Marks a contract that requests a single result from a mediator handler.
/// Each request type is bound to exactly one handler that produces a <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The result type returned by the handler.</typeparam>
public interface IRequest<out TResult>;

/// <summary>
/// Implemented by a contract to provide a custom identity key consumed by replay, cache, and other
/// middleware that need to determine uniqueness of an <see cref="IRequest{TResult}"/> or
/// <see cref="IStreamRequest{TResult}"/> instance.
/// </summary>
public interface IContractKey
{
    /// <summary>
    /// Returns the key used by middleware (cache, replay, etc.) to identify this contract instance.
    /// </summary>
    /// <returns>A string uniquely identifying the contract for storage and lookup purposes.</returns>
    string GetKey();
}