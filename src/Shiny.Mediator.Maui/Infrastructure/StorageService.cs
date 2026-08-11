using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// MAUI file-based <see cref="IStorageService"/> implementation that persists serialized
/// values into MAUI's <see cref="IFileSystem.CacheDirectory"/>. Suitable for backing
/// <see cref="StorageCacheService"/> with cache that survives app restarts.
/// </summary>
public class StorageService(
    IFileSystem fileSystem,
    Shiny.ISerializer serializer,
    ILogger<StorageService>? logger = null
) : AbstractFileStorageService(serializer, logger)
{
    /// <summary>
    /// Root directory used for backing files. Defaults to <see cref="IFileSystem.CacheDirectory"/>;
    /// override to relocate storage.
    /// </summary>
    protected virtual string StoreDirectory => fileSystem.CacheDirectory;
    
    
    /// <inheritdoc/>
    protected override Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
    {
        var path = Path.Combine(this.StoreDirectory, fileName);
        return File.WriteAllTextAsync(path, content, cancellationToken);
    }


    /// <inheritdoc/>
    protected override async Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(this.StoreDirectory, fileName);
        if (!File.Exists(path))
            return null;

        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return content;
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