using System.IO;
using Windows.Storage;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Uno file-based <see cref="IStorageService"/> implementation that persists serialized values
/// to <see cref="ApplicationData.LocalFolder"/>. Suitable for backing
/// <see cref="StorageCacheService"/> with cache that survives app restarts.
/// </summary>
public class StorageService(
    Shiny.ISerializer serializer,
    ILogger<StorageService>? logger = null
) : AbstractFileStorageService(serializer, logger)
{
    /// <inheritdoc/>
    protected override async Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
    {
        var local = ApplicationData.Current.LocalFolder;
        var file = await local.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(file, content);
    }


    /// <inheritdoc/>
    protected override async Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
    {
        var local = ApplicationData.Current.LocalFolder;
        var fn = Path.Combine(local.Path, fileName);
        if (!File.Exists(fn))
            return null;

        var file = await local.GetFileAsync(fn);
        var content = await FileIO.ReadTextAsync(file);
        return content;
    }


    /// <inheritdoc/>
    protected override async Task DeleteFile(string fileName, CancellationToken cancellationToken)
    {
        var local = ApplicationData.Current.LocalFolder;
        var fn = Path.Combine(local.Path, fileName);
        if (File.Exists(fn))
        {
            var file = await local.GetFileAsync(fileName);
            await file.DeleteAsync();
        }
    }
}