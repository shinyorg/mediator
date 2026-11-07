using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Avalonia file-based <see cref="IStorageService"/> implementation that persists serialized values
/// under <see cref="Environment.SpecialFolder.LocalApplicationData"/> in a folder named after the
/// entry assembly. Suitable for backing <see cref="StorageCacheService"/> with cache that survives
/// app restarts.
/// </summary>
public class StorageService(
    Shiny.ISerializer serializer,
    ILogger<StorageService>? logger = null
) : AbstractFileStorageService(serializer, logger)
{
    /// <summary>
    /// Root directory used for backing files. Defaults to
    /// <c>LocalApplicationData/&lt;entry assembly name&gt;/mediator</c>; override to relocate storage.
    /// </summary>
    protected virtual string StoreDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        Assembly.GetEntryAssembly()?.GetName().Name ?? "ShinyMediator",
        "mediator"
    );


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
