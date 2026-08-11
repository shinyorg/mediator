using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


/// <summary>
/// Blazor <see cref="IStorageService"/> that persists serialized values to browser storage via
/// the <c>MediatorServices</c> JS module (typically localStorage). Keys are namespaced as
/// <c>{category}_{key}</c>.
/// </summary>
public class StorageService(
    IJSRuntime jsruntime,
    Shiny.ISerializer serializer,
    ILogger<StorageService>? logger = null
) : IStorageService
{
    /// <inheritdoc/>
    public async Task Set<T>(string category, string key, T value, CancellationToken cancellationToken)
    {
        logger?.LogInformation("Storing {Category}-{key}", category, key);
        var content = serializer.Serialize(value);
        var requestKey = $"{category}_{key}";
        await jsruntime
            .InvokeVoidAsync(
                "MediatorServices.setStore", 
                cancellationToken, 
                requestKey, 
                content
            );
    }

    
    /// <inheritdoc/>
    public async Task<T?> Get<T>(string category, string key, CancellationToken cancellationToken)
    {
        var content = await jsruntime.InvokeAsync<string?>(
            "MediatorServices.getStore", 
            cancellationToken,
            this.GetKey(category, key)
        );
        if (String.IsNullOrWhiteSpace(content))
            return default;

        var obj = serializer.Deserialize<T>(content);
        return obj;
    }

    /// <inheritdoc/>
    public async Task Remove(string category, string requestKey, bool partialMatchKey = false, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Evicting {Category}-{key}", category, requestKey);
        await jsruntime.InvokeVoidAsync("MediatorServices.removeStore", cancellationToken, this.GetKey(category, requestKey));
    }

    
    /// <inheritdoc/>
    public async Task Clear(string category, CancellationToken cancellationToken = default)
    {
        await jsruntime.InvokeVoidAsync("MediatorServices.clearStore", cancellationToken);
    }
    
    
    /// <summary>
    /// Composes the storage key used in the underlying JS store. Defaults to <c>{category}_{key}</c>;
    /// override to change key formatting.
    /// </summary>
    protected virtual string GetKey(string category, string key) => $"{category}_{key}";
}