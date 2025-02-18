namespace Shiny.Mediator.Infrastructure;


public interface IOfflineService
{
    Task<string> Set(object request, object result);
    Task<OfflineResult<TResult>?> Get<TResult>(object request);
    Task RemoveByKey(string key);
    Task Remove(Type? type = null, string? keyPrefix = null);
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
        var requestKey = Utils.GetRequestKey(request);
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
        var requestKey = Utils.GetRequestKey(request);
        var store = await storage
            .Get<OfflineStore>(Category, requestKey)
            .ConfigureAwait(false);
        
        if (store == null)
            return null;

        var obj = serializer.Deserialize<TResult>(store.Json);
        return new OfflineResult<TResult>(store.RequestKey, store.Timestamp, obj);
    }

    public Task RemoveByKey(string key)
        => storage.RemoveByKey(Category, key);

    public Task Remove(Type? type = null, string? keyPrefix = null)
        => storage.Remove(Category, type, keyPrefix);

    public Task Clear() => storage.Remove(Category, null, null);

    string GetTypeKey(Type type) => $"{type.Namespace}.{type.Name}";
}

public record OfflineStore(
    string TypeName,
    string RequestKey,
    DateTimeOffset Timestamp,
    string Json
);