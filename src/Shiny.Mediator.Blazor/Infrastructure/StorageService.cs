using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class StorageService(
    ILogger<StorageService> logger, 
    IJSRuntime jsruntime, 
    ISerializerService serializer
) : IStorageService
{
    public async Task Set<T>(string category, string key, T value)
    {
        logger.LogInformation("Storing {Category}-{key}", category, key);
        var content = serializer.Serialize(value);
        var requestKey = $"{category}_{key}";
        await jsruntime.InvokeVoidAsync("MediatorServices.setStore", requestKey, content);
    }

    
    public async Task<T?> Get<T>(string category, string key)
    {
        var content = await jsruntime.InvokeAsync<string?>(
            "MediatorServices.getStore", 
            this.GetKey(category, key)
        );
        if (String.IsNullOrWhiteSpace(content))
            return default;

        var obj = serializer.Deserialize<T>(content);
        return obj;
    }

    public async Task Remove(string category, string requestKey, bool partialMatchKey = false)
    {
        logger.LogInformation("Evicting {Category}-{key}", category, requestKey);
        await jsruntime.InvokeVoidAsync("MediatorServices.removeStore", this.GetKey(category, requestKey));
    }

    
    public Task Clear(string category)
    {
        throw new NotImplementedException();
    }
    
    
    protected virtual string GetKey(string category, string key) => $"{category}_{key}";
}