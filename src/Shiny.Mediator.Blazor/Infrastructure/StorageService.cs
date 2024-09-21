using System.Text.Json;
using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class StorageService(IJSRuntime jsruntime) : IStorageService
{
    public Task Store(object request, object result, bool isPeristent)
    {
        var key = this.GetStoreKeyFromRequest(request);
        var store = isPeristent ? "localStorage" : "sessionStorage";

        var json = JsonSerializer.Serialize(result);
        ((IJSInProcessRuntime)jsruntime).Invoke<string?>(store + ".setItem", key, json);

        return Task.CompletedTask;
    }

    
    public Task<TResult?> Get<TResult>(object request, bool isPeristent)
    {
        var key = this.GetStoreKeyFromRequest(request);
        var store = isPeristent ? "localStorage" : "sessionStorage";
        var stringValue = ((IJSInProcessRuntime)jsruntime).Invoke<string?>(store + ".getItem", key);
        if (String.IsNullOrWhiteSpace(stringValue))
            return null!;

        var final = JsonSerializer.Deserialize<TResult>(stringValue);
        return Task.FromResult(final);
    }

    public Task Clear()
    {
        var inproc = (IJSInProcessRuntime)jsruntime;
        inproc.InvokeVoid("localStorage.clear");
        inproc.InvokeVoid("sessionStorage.clear");
        return Task.CompletedTask;
    }
    
    
    protected virtual string GetStoreKeyFromRequest(object request)
    {
        if (request is IRequestKey keyProvider)
            return keyProvider.GetKey();
        
        var t = request.GetType();
        var key = $"{t.Namespace}_{t.Name}";
    
        return key;
    }
}