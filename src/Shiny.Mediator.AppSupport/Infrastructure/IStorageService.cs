namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Persistent key/value storage abstraction used by the offline and cache subsystems. Values are partitioned by
/// a category (e.g. <c>Offline</c>, <c>Cache</c>) so independent modules can safely share request key namespaces.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Stores <paramref name="value"/> under the given category and key, overwriting any existing entry.
    /// </summary>
    /// <param name="category">Logical partition such as <c>Offline</c> or <c>Cache</c>.</param>
    /// <param name="key">The request key identifying the entry within the category.</param>
    /// <param name="value">The value to serialize and persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task Set<T>(string category, string key, T value, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a previously stored value, or returns <c>default</c> when no entry exists for the key.
    /// </summary>
    /// <param name="category">Logical partition such as <c>Offline</c> or <c>Cache</c>.</param>
    /// <param name="key">The request key identifying the entry within the category.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<T?> Get<T>(string category, string key, CancellationToken cancellationToken);

    /// <summary>
    /// Removes one or more entries from a category.
    /// </summary>
    /// <param name="category">Logical partition such as <c>Offline</c> or <c>Cache</c>.</param>
    /// <param name="requestKey">The request key to match.</param>
    /// <param name="partialMatchKey">When <c>true</c>, removes every entry whose key starts with <paramref name="requestKey"/>; otherwise only exact matches are removed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task Remove(string category, string requestKey, bool partialMatchKey = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every entry in the given category.
    /// </summary>
    Task Clear(string category, CancellationToken cancellationToken);
}
