namespace Shiny.Mediator.Infrastructure;


public interface IOfflineService
{
    Task<string> Set(object request, object result);
    Task<OfflineResult<TResult>?> Get<TResult>(object request);
    Task Remove(string requestKey, bool partialMatch);
    Task Clear();
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
    
    public async Task<string> Set(object request, object result)
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
                )
            )
            .ConfigureAwait(false);
        
        return requestKey;
    }

    public async Task<OfflineResult<TResult>?> Get<TResult>(object request)
    {
        var requestKey = ContractUtils.GetRequestKey(request);
        var store = await storage
            .Get<OfflineStore>(Category, requestKey)
            .ConfigureAwait(false);
        
        if (store == null)
            return null;

        var obj = serializer.Deserialize<TResult>(store.Json);
        return new OfflineResult<TResult>(store.RequestKey, store.Timestamp, obj);
    }

    public Task Remove(string requestKey, bool partialMatch = false)
        => storage.Remove(Category, requestKey, partialMatch);

    public Task Clear() => storage.Clear(Category);

    string GetTypeKey(Type type) => $"{type.Namespace}.{type.Name}";
}

public record OfflineStore(
    string TypeName,
    string RequestKey,
    DateTimeOffset Timestamp,
    string Json
);