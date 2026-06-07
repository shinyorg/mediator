namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Persists and retrieves the last successful result for a request so that subsequent calls can return a
/// cached value when the network is unavailable. Backed by <see cref="IStorageService"/>.
/// </summary>
public interface IOfflineService
{
    /// <summary>
    /// Persists <paramref name="result"/> as the offline payload for <paramref name="request"/> and returns
    /// the contract key used to store it.
    /// </summary>
    Task<string> Set(object request, object result, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the stored offline result for <paramref name="request"/>, or <c>null</c> when no entry exists.
    /// </summary>
    Task<OfflineResult<TResult>?> Get<TResult>(object request, CancellationToken cancellationToken);

    /// <summary>
    /// Removes one or more offline entries by request key.
    /// </summary>
    /// <param name="requestKey">The request key to match.</param>
    /// <param name="partialMatch">When <c>true</c>, removes every entry whose key starts with <paramref name="requestKey"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task Remove(string requestKey, bool partialMatch, CancellationToken cancellationToken);

    /// <summary>
    /// Clears every offline entry across all requests.
    /// </summary>
    Task Clear(CancellationToken cancellationToken);
}

/// <summary>
/// Result returned by <see cref="IOfflineService.Get{TResult}"/> when a stored value exists.
/// </summary>
/// <param name="RequestKey">The contract key the entry was stored under.</param>
/// <param name="Timestamp">UTC timestamp recording when the value was originally stored.</param>
/// <param name="Value">The deserialized value.</param>
public record OfflineResult<TResult>(
    string RequestKey,
    DateTimeOffset Timestamp,
    TResult Value
);

/// <summary>
/// Default <see cref="IOfflineService"/> implementation that serializes results through
/// <see cref="Shiny.ISerializer"/> and persists them via <see cref="IStorageService"/> under the
/// <see cref="Category"/> partition.
/// </summary>
public class OfflineService(
    IStorageService storage,
    Shiny.ISerializer serializer,
    TimeProvider timeProvider,
    IContractKeyProvider contractKeyProvider
) : IOfflineService
{
    /// <summary>
    /// The storage partition name used for offline entries.
    /// </summary>
    public const string Category = "Offline";
    
    /// <inheritdoc/>
    public async Task<string> Set(object request, object result, CancellationToken cancellationToken)
    {
        var requestKey = contractKeyProvider.GetContractKey(request);
        await storage
            .Set(
                Category,
                requestKey, 
                new OfflineStore(
                    this.GetTypeKey(request.GetType()),
                    requestKey,
                    timeProvider.GetUtcNow(),
                    serializer.Serialize(result)
                ),
                cancellationToken
            )
            .ConfigureAwait(false);
        
        return requestKey;
    }

    /// <inheritdoc/>
    public async Task<OfflineResult<TResult>?> Get<TResult>(object request, CancellationToken cancellationToken)
    {
        var requestKey = contractKeyProvider.GetContractKey(request);
        var store = await storage
            .Get<OfflineStore>(Category, requestKey, cancellationToken)
            .ConfigureAwait(false);
        
        if (store == null)
            return null;

        var obj = serializer.Deserialize<TResult>(store.Json);
        return new OfflineResult<TResult>(store.RequestKey, store.Timestamp, obj);
    }

    /// <inheritdoc/>
    public Task Remove(string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
        => storage.Remove(Category, requestKey, partialMatch, cancellationToken);

    /// <inheritdoc/>
    public Task Clear(CancellationToken cancellationToken) => storage.Clear(Category, cancellationToken);

    string GetTypeKey(Type type) => $"{type.Namespace}.{type.Name}";
}

/// <summary>
/// Serializable envelope persisted by <see cref="OfflineService"/> capturing the request type, contract key,
/// timestamp, and JSON-serialized result value.
/// </summary>
public record OfflineStore(
    string TypeName,
    string RequestKey,
    DateTimeOffset Timestamp,
    string Json
);