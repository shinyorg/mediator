using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class StorageService(IJSRuntime jsruntime, ISerializerService serializer) : IStorageService
{
    public Task Set<T>(string key, T value)
    {
        var json = serializer.Serialize(value);
        ((IJSInProcessRuntime)jsruntime).Invoke<string?>("MediatorServices.setStore", key, json);

        return Task.FromResult(key);
    }

    
    public Task<T?> Get<T>(string key)
    {
        var stringValue = ((IJSInProcessRuntime)jsruntime).Invoke<string?>("MediatorServices.getStore", key);
        if (String.IsNullOrWhiteSpace(stringValue))
            return Task.FromResult<T?>(default);

        var final = serializer.Deserialize<T>(stringValue);
        return Task.FromResult<T?>(final);
    }

    
    public Task Remove(string key)
    {
        var inproc = (IJSInProcessRuntime)jsruntime;
        inproc.InvokeVoid("MediatorServices.removeStore", key);
        return Task.CompletedTask;
    }
}