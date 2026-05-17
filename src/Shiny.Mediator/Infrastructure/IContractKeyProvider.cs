namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Produces a stable string key for a contract instance. Used by caching, offline storage, and replay
/// middleware to uniquely identify a request/command/event payload. Implementations may honour
/// <see cref="IContractKey"/> on the contract itself or fall back to reflection.
/// </summary>
public interface IContractKeyProvider
{
    /// <summary>
    /// Returns a string key that uniquely identifies <paramref name="contract"/> for cache and storage purposes.
    /// </summary>
    string GetContractKey(object contract);
}
