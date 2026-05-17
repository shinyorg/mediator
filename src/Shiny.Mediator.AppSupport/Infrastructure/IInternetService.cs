namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Platform abstraction for observing and querying network reachability. Used by the offline and replay middleware
/// to decide whether to call the handler or fall back to stored results.
/// </summary>
public interface IInternetService
{
    /// <summary>
    /// Raised when network reachability transitions. The boolean argument is <c>true</c> when connected.
    /// </summary>
    event EventHandler<bool> StateChanged;

    /// <summary>
    /// Gets the current reachability state. <c>true</c> if the device currently has internet access.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Returns a task that completes when the device next has internet access. Completes immediately when
    /// <see cref="IsAvailable"/> is already <c>true</c>.
    /// </summary>
    Task WaitForAvailable(CancellationToken cancelToken = default);
}