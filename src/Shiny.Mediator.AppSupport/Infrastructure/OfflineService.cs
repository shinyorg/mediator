namespace Shiny.Mediator.Infrastructure;


public interface IOfflineService
{
    Task<string> Set(object request, object result, CancellationToken cancellationToken);
    Task<OfflineResult<TResult>?> Get<TResult>(object request, CancellationToken cancellationToken);
    Task Remove(string requestKey, bool partialMatch, CancellationToken cancellationToken);
    Task Clear(CancellationToken cancellationToken);
}

public record OfflineResult<TResult>(
    string RequestKey,
    DateTimeOffset Timestamp,
    TResult Value
);

public class OfflineService(
    IStorageService storage, 
    ISerializerService serializer,
    TimeProvider timeProvider
) : IOfflineService
{
    public const string Category = "Offline";
    
    public async Task<string> Set(object request, object result, CancellationToken cancellationToken)
    {
        var requestKey = ContractUtils.GetRequestKey(request);
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

    public async Task<OfflineResult<TResult>?> Get<TResult>(object request, CancellationToken cancellationToken)
    {
        var requestKey = ContractUtils.GetRequestKey(request);
        var store = await storage
            .Get<OfflineStore>(Category, requestKey, cancellationToken)
            .ConfigureAwait(false);
        
        if (store == null)
            return null;

        var obj = serializer.Deserialize<TResult>(store.Json);
        return new OfflineResult<TResult>(store.RequestKey, store.Timestamp, obj);
    }

    public Task Remove(string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
        => storage.Remove(Category, requestKey, partialMatch, cancellationToken);

    public Task Clear(CancellationToken cancellationToken) => storage.Clear(Category, cancellationToken);

    string GetTypeKey(Type type) => $"{type.Namespace}.{type.Name}";
}

public record OfflineStore(
    string TypeName,
    string RequestKey,
    DateTimeOffset Timestamp,
    string Json
);