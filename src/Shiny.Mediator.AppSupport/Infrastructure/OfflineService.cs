namespace Shiny.Mediator.Infrastructure;


public interface IOfflineService
{
    Task<string> Set(object request, object result);
    Task<OfflineResult<TResult>?> Get<TResult>(object request);
    Task ClearByType(Type requestType);
    Task ClearByRequest(object request);
    Task Clear();
}

public record OfflineResult<TResult>(
    string RequestKey,
    DateTimeOffset Timestamp,
    TResult Value
);

public class OfflineService(IStorageService storage, ISerializerService serializer) : IOfflineService
{
    public async Task<string> Set(object request, object result)
    {
        var requestKey = this.GetRequestKey(request);
        await this.DoTransaction(dict =>
        {
            dict[requestKey] = new OfflineStore(
                this.GetTypeKey(request.GetType()),
                requestKey,
                DateTimeOffset.UtcNow,
                serializer.Serialize(result)
            );
            return true;
        });
        return requestKey;
    }


    public async Task<OfflineResult<TResult>?> Get<TResult>(object request)
    {
        OfflineResult<TResult>? result = null;
        await this
            .DoTransaction(dict =>
            {
                var requestKey = this.GetRequestKey(request);
                if (dict.TryGetValue(requestKey, out var store))
                {
                    var jsonObj = serializer.Deserialize<TResult>(store.Json);
                    result = new OfflineResult<TResult>(store.RequestKey, store.Timestamp, jsonObj);
                }
                return false;
            })
            .ConfigureAwait(false);

        return result;
    }


    public Task ClearByType(Type requestType) => this.DoTransaction(dict =>
    {
        var typeKey = this.GetTypeKey(requestType);
        var keys = dict
            .Where(x => x.Value.TypeName == typeKey)
            .Select(x => x.Key)
            .ToList();

        if (keys.Count == 0)
            return false;

        foreach (var key in keys)
            dict.Remove(key);
        
        return false;
    });

    
    public Task ClearByRequest(object request) => this.DoTransaction(dict =>
    {
        var requestKey = this.GetRequestKey(request);
        if (dict.ContainsKey(requestKey))
        {
            dict.Remove(requestKey);
            return true;
        }
        return false;
    });


    public Task Clear() => this.DoTransaction(dict =>
    {
        dict.Clear();
        return true;
    });


    SemaphoreSlim semaphore = new(1, 1);
    Dictionary<string, OfflineStore> cache = null!;
    Task DoTransaction(Func<IDictionary<string, OfflineStore>, bool> action) => Task.Run(async () =>
    {
        await this.semaphore.WaitAsync();
        if (this.cache == null)
        {
            var dict = await storage
                .Get<Dictionary<string, OfflineStore>>(nameof(IStorageService))
                .ConfigureAwait(false);

            this.cache = dict ?? new();
        }
        var result = action(this.cache);
        if (result)
            await storage.Set(nameof(IStorageService), this.cache).ConfigureAwait(false);

        this.semaphore.Release();
    });

    
    string GetRequestKey(object request)
    {
        if (request is IRequestKey keyProvider)
            return keyProvider.GetKey();
        
        var t = request.GetType();
        var key = $"{t.Namespace}_{t.Name}";

        return key;
    }


    string GetTypeKey(Type type) => $"{type.Namespace}.{type.Name}";
}

record OfflineStore(
    string TypeName,
    string RequestKey,
    DateTimeOffset Timestamp,
    string Json
);