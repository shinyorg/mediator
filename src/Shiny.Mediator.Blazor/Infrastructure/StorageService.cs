using System.Text.Json;
using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class StorageService(IJSRuntime jsruntime) : IStorageService
{
    public Task Set<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        ((IJSInProcessRuntime)jsruntime).Invoke<string?>("localStorage.setItem", key, json);

        return Task.FromResult(key);
    }

    
    public Task<T> Get<T>(string key)
    {
        var stringValue = ((IJSInProcessRuntime)jsruntime).Invoke<string?>("localStorage.getItem", key);
        if (String.IsNullOrWhiteSpace(stringValue))
            return null!;

        var final = JsonSerializer.Deserialize<T>(stringValue);
        return Task.FromResult(final);
    }

    
    public Task Remove(string key)
    {
        var inproc = (IJSInProcessRuntime)jsruntime;
        inproc.InvokeVoid("localStorage.removeItem", key);
        return Task.CompletedTask;
    }
}