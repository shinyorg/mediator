using System.IO;
using Windows.Storage;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Uno Platform file-based <see cref="IStorageService"/> implementation that persists serialized values
/// to <see cref="ApplicationData.LocalFolder"/>. Suitable for backing
/// <see cref="StorageCacheService"/> with cache that survives app restarts.
/// </summary>
public class StorageService(
    Shiny.ISerializer serializer,
    ILogger<StorageService>? logger = null
) : AbstractFileStorageService(serializer, logger)
{
    /// <summary>
    /// Root directory used for backing files. Defaults to <see cref="ApplicationData.LocalFolder"/>;
    /// override to relocate storage.
    /// </summary>
    protected virtual string StoreDirectory => ApplicationData.Current.LocalFolder.Path;


    /// <inheritdoc/>
    protected override Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(this.StoreDirectory);
        var path = Path.Combine(this.StoreDirectory, fileName);
        return File.WriteAllTextAsync(path, content, cancellationToken);
    }


    /// <inheritdoc/>
    protected override async Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(this.StoreDirectory, fileName);
        if (!File.Exists(path))
            return null;

        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }


    /// <inheritdoc/>
    protected override Task DeleteFile(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(this.StoreDirectory, fileName);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
