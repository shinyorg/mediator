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
/// Request a stream of data from a handler
/// </summary>
/// <typeparam name="TResult"></typeparam>
public interface IStreamRequest<out TResult>; 

/// <summary>
/// Request data from a mediator handler
/// </summary>
/// <typeparam name="TResult"></typeparam>
public interface IRequest<out TResult>;

/// <summary>
/// This is viewed by replay, cache, and various other services where you can control an entry
/// Simply mark your IRequest or IStreamRequest and provide the necessary key to determine uniqueness
/// </summary>
public interface IContractKey
{
    /// <summary>
    /// Return your custom key to determine how this contract response is cached or replayed
    /// </summary>
    /// <returns></returns>
    string GetKey();
}