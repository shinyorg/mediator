using System.Text.Json;
using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class StorageService(IJSRuntime jsruntime) : IStorageService
{
    public Task Store(object request, object result)
    {
        var key = this.GetStoreKeyFromRequest(request);
        var json = JsonSerializer.Serialize(result);
        ((IJSInProcessRuntime)jsruntime).Invoke<string?>("localStorage.setItem", key, json);

        return Task.CompletedTask;
    }

    
    public Task<TResult?> Get<TResult>(object request)
    {
        var key = this.GetStoreKeyFromRequest(request);
        var stringValue = ((IJSInProcessRuntime)jsruntime).Invoke<string?>("localStorage.getItem", key);
        if (String.IsNullOrWhiteSpace(stringValue))
            return null!;

        var final = JsonSerializer.Deserialize<TResult>(stringValue);
        return Task.FromResult(final);
    }

    public Task Clear()
    {
        var inproc = (IJSInProcessRuntime)jsruntime;
        inproc.InvokeVoid("localStorage.clear");
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