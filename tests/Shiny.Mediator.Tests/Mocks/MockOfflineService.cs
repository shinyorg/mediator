using System.Text.Json;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Mocks;


public class MockOfflineService(TimeProvider timeProvider) : IOfflineService
{
    readonly Dictionary<string, object> data = new();
    
    public Task<string> Set(object request, object result, CancellationToken cancellationToken = default)
    {
        var key = ContractUtils.GetRequestKey(request);
        var json = JsonSerializer.Serialize(result);
        
        this.data[key] = new OfflineStore(
            request.GetType().FullName,
            key,
            timeProvider.GetUtcNow(),
            json
        );
        return Task.FromResult(key);
    }

    public Task<OfflineResult<TResult>?> Get<TResult>(object request, CancellationToken cancellationToken = default)
    {
        var key = ContractUtils.GetRequestKey(request);
        if (!this.data.ContainsKey(key))
            return null;

        var store = (OfflineStore)this.data[key];
        var obj = JsonSerializer.Deserialize<TResult>(store.Json);
        
        return Task.FromResult(new OfflineResult<TResult>(key, store.Timestamp, obj));
    }

    public Task Remove(string requestKey, bool partialMatch, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task Clear(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    // public Task ClearByType(Type requestType)
    // {
    //     var tn = requestType.GetType().FullName;
    //     foreach (var key in this.data.Keys)
    //     {
    //         var store = (OfflineStore)this.data[key];
    //         if (store.TypeName == tn) 
    //             this.data.Remove(key);
    //     }
    //     return Task.CompletedTask;
    // }
    //
    // public Task ClearByRequest(object request)
    // {
    //     var key = ContractUtils.GetObjectKey(request);
    //     this.data.Remove(key);
    //     return Task.CompletedTask;
    // }
    //
    // public Task Clear()
    // {
    //     this.data.Clear();
    //     return Task.CompletedTask;   
    // }
}