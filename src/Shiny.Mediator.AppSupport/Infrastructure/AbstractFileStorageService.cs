using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Abstract file-backed <see cref="IStorageService"/>. Subclasses provide the
/// platform-specific file I/O via <see cref="WriteFile"/>, <see cref="ReadFile"/>,
/// and <see cref="DeleteFile"/>. The shared index file is guarded by a single
/// semaphore, while each <c>(category, key)</c> pair has its own
/// <see cref="KeyedLocker"/> lock so concurrent writes to different keys
/// proceed in parallel without tearing.
/// </summary>
public abstract class AbstractFileStorageService(
    Shiny.ISerializer serializer,
    ILogger? logger = null
) : IStorageService
{
    /// <summary>Writes <paramref name="content"/> to the platform store under <paramref name="fileName"/>, overwriting any existing data.</summary>
    protected abstract Task WriteFile(string fileName, string content, CancellationToken cancellationToken);

    /// <summary>Reads the contents of <paramref name="fileName"/> from the platform store; returns <c>null</c> when the file is absent.</summary>
    protected abstract Task<string?> ReadFile(string fileName, CancellationToken cancellationToken);

    /// <summary>Deletes <paramref name="fileName"/> from the platform store. No-op when the file is absent.</summary>
    protected abstract Task DeleteFile(string fileName, CancellationToken cancellationToken);


    /// <inheritdoc/>
    public async Task Set<T>(string category, string key, T value, CancellationToken cancellationToken)
    {
        var fileName = await this.GetFileIndexer(category, key, cancellationToken).ConfigureAwait(false);
        using (await this.keyLocker.LockAsync(LockKey(category, key), cancellationToken).ConfigureAwait(false))
        {
            logger?.LogInformation("Setting {Category}-{key} to {File}", category, key, fileName);
            await this.WriteObject(fileName, value, cancellationToken).ConfigureAwait(false);
            await this.WriteState(cancellationToken).ConfigureAwait(false);
        }
    }


    /// <inheritdoc/>
    public async Task<T?> Get<T>(string category, string key, CancellationToken cancellationToken)
    {
        var fileName = await this.GetFileIndexer(category, key, cancellationToken).ConfigureAwait(false);
        T? obj = default;
        var recover = false;

        using (await this.keyLocker.LockAsync(LockKey(category, key), cancellationToken).ConfigureAwait(false))
        {
            try
            {
                obj = await this.GetObject<T>(fileName, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting {Category}-{Key}", category, key);
                recover = true;
            }
        }

        if (recover)
            await this.Remove(category, key, cancellationToken: cancellationToken).ConfigureAwait(false);

        return obj;
    }


    /// <inheritdoc/>
    public async Task Remove(string category, string requestKey, bool partialMatch = false, CancellationToken cancellationToken = default)
    {
        var indexes = await this.GetIndexCategory(category, cancellationToken).ConfigureAwait(false);

        if (!partialMatch)
        {
            if (indexes.TryGetValue(requestKey, out var fileName))
            {
                using (await this.keyLocker.LockAsync(LockKey(category, requestKey), cancellationToken).ConfigureAwait(false))
                {
                    await this.DoRemove(indexes, category, requestKey, fileName, true, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else
        {
            var copy = indexes.ToList();
            var changed = false;

            foreach (var (key, value) in copy)
            {
                if (!String.IsNullOrWhiteSpace(key) && key.StartsWith(requestKey))
                {
                    changed = true;
                    using (await this.keyLocker.LockAsync(LockKey(category, key), cancellationToken).ConfigureAwait(false))
                    {
                        await this
                            .DoRemove(indexes, category, key, value, false, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            if (changed)
                await this.WriteState(cancellationToken).ConfigureAwait(false);
        }
    }


    /// <inheritdoc/>
    public async Task Clear(string category, CancellationToken cancellationToken)
    {
        var indexes = await this.GetIndexCategory(category, cancellationToken).ConfigureAwait(false);
        if (indexes.Count > 0)
        {
            var copy = indexes.ToList();
            foreach (var (key, fn) in copy)
            {
                using (await this.keyLocker.LockAsync(LockKey(category, key), cancellationToken).ConfigureAwait(false))
                {
                    await this.DoRemove(indexes, category, key, fn, false, cancellationToken).ConfigureAwait(false);
                }
            }

            indexes.Clear();
            await this.WriteState(cancellationToken).ConfigureAwait(false);
        }
    }


    async Task DoRemove(
        ConcurrentDictionary<string, string> indexes,
        string category,
        string requestKey,
        string fileName,
        bool writeState,
        CancellationToken cancellationToken
    )
    {
        logger?.LogInformation("Evicting {RequestKey} in {Category}", requestKey, category);
        indexes.TryRemove(requestKey, out _);
        await this.DeleteFile(fileName, cancellationToken).ConfigureAwait(false);
        if (writeState)
            await this.WriteState(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes <paramref name="value"/> via <see cref="Shiny.ISerializer"/> and writes
    /// the result to <paramref name="fileName"/> through <see cref="WriteFile"/>.
    /// </summary>
    protected virtual async Task WriteObject<T>(string fileName, T value, CancellationToken cancellationToken)
    {
        var content = serializer.Serialize(value);
        await this.WriteFile(fileName, content, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Reads <paramref name="fileName"/> via <see cref="ReadFile"/> and deserializes the
    /// contents into <typeparamref name="T"/>. Returns <c>default</c> when the file is empty or missing.
    /// </summary>
    protected virtual async Task<T?> GetObject<T>(string fileName, CancellationToken cancellationToken)
    {
        var content = await this.ReadFile(fileName, cancellationToken).ConfigureAwait(false);
        if (String.IsNullOrWhiteSpace(content))
        {
            logger?.LogInformation("No content found for {FileName}", fileName);
            return default;
        }

        var obj = serializer.Deserialize<T>(content);
        return obj;
    }


    /// <summary>
    /// Persists the in-memory category/key → file-name index to <see cref="IndexFile"/>
    /// under the shared semaphore. Called after mutations to keep the on-disk index in sync.
    /// </summary>
    protected virtual async Task WriteState(CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this._indexes != null)
            {
                logger?.LogInformation("Writing File Index");
                await this.WriteObject(IndexFile, this._indexes, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            this.semaphore.Release();
        }
    }


    static string LockKey(string category, string key) => category + "" + key;

    /// <summary>File name used to persist the category/key → file-name index.</summary>
    protected const string IndexFile = "indexes.mediator";
    readonly SemaphoreSlim semaphore = new(1, 1);
    readonly KeyedLocker keyLocker = new();
    ConcurrentDictionary<string, ConcurrentDictionary<string, string>>? _indexes;

    /// <summary>
    /// Returns the key → file-name map for <paramref name="category"/>, lazily loading
    /// the index from disk on first access. Subsequent calls return the cached in-memory copy.
    /// </summary>
    protected async Task<ConcurrentDictionary<string, string>> GetIndexCategory(string category, CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            this._indexes ??= await this
                .GetObject<ConcurrentDictionary<string, ConcurrentDictionary<string, string>>>(IndexFile, cancellationToken)
                .ConfigureAwait(false);

            this._indexes ??= new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            return this._indexes.GetOrAdd(category, _ => new ConcurrentDictionary<string, string>());
        }
        finally
        {
            this.semaphore.Release();
        }
    }


    /// <summary>
    /// Returns the on-disk file name for <paramref name="key"/> in <paramref name="category"/>,
    /// allocating a new <see cref="Guid"/>-based name and recording it in the index on first use.
    /// </summary>
    protected async Task<string> GetFileIndexer(string category, string key, CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var indexes = await this.GetIndexCategoryUnsafe(category, cancellationToken).ConfigureAwait(false);
            return indexes.GetOrAdd(key, _ => Guid.NewGuid().ToString());
        }
        finally
        {
            this.semaphore.Release();
        }
    }


    async Task<ConcurrentDictionary<string, string>> GetIndexCategoryUnsafe(string category, CancellationToken cancellationToken)
    {
        this._indexes ??= await this
            .GetObject<ConcurrentDictionary<string, ConcurrentDictionary<string, string>>>(IndexFile, cancellationToken)
            .ConfigureAwait(false);

        this._indexes ??= new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        return this._indexes.GetOrAdd(category, _ => new ConcurrentDictionary<string, string>());
    }
}
