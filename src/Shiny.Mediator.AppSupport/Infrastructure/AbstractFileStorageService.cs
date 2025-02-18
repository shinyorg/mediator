using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public abstract class AbstractFileStorageService(
    ISerializerService serializer,
    ILogger logger
) : IStorageService
{
    protected abstract Task WriteFile(string fileName, string content);
    protected abstract Task<string?> ReadFile(string fileName);
    protected abstract Task DeleteFile(string fileName);
    
    
    public async Task Set<T>(string category, string key, T value)
    {
        var fileName = await this.GetFileIndexer(category, key).ConfigureAwait(false);
        logger.LogInformation("Setting {Category}-{key} to {File}", category, key, fileName);
        await this.WriteObject(fileName, value).ConfigureAwait(false);
    }


    public async Task<T?> Get<T>(string category, string key)
    {
        var fileName = await this.GetFileIndexer(category, key).ConfigureAwait(false);
        var obj = await this.GetObject<T>(fileName);
        return obj;
    }


    public async Task RemoveByKey(string category, string key)
    {
        var indexes = await this.GetIndexCategory(category).ConfigureAwait(false);
        
        if (indexes.TryGetValue(key, out var fileName))
        {
            await this.DoRemove(indexes, category, key, fileName).ConfigureAwait(false);
        }
    }
    

    public async Task Remove(string category, Type? type = null, string? prefix = null)
    {
        var indexes = await this.GetIndexCategory(category).ConfigureAwait(false);
        var copy = indexes.ToList();
        var changed = false;

        foreach (var (key, value) in copy)
        {
            if (!String.IsNullOrWhiteSpace(key) && key.StartsWith(prefix))
            {
                changed = true;
                await this
                    .DoRemove(indexes, category, key, value, false)
                    .ConfigureAwait(false);
            }
            // else if {}
            // TODO: I need to write type somehow if I want to delete it!?
        }

        if (changed)
            await this.WriteState().ConfigureAwait(false);
    }


    async Task DoRemove(
        ConcurrentDictionary<string, string> indexes, 
        string category, 
        string requestKey, 
        string fileName, 
        bool writeState = true
    )
    {
        logger.LogInformation("Evicting {RequestKey} in {Category}", requestKey, category);
        indexes.TryRemove(requestKey, out _);
        await this.DeleteFile(fileName).ConfigureAwait(false);
        if (writeState)
            await this.WriteState().ConfigureAwait(false);
    }

    // public async Task RemoveByPrefix(string prefix)
    // {
    //     var changed = false;
    //     var indexes = await this.GetFileIndexes().ConfigureAwait(false);
    //     var indexCopy = indexes.ToList();
    //     
    //     foreach (var (key, value) in indexCopy)
    //     {
    //         if (key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
    //         {
    //             await this.DeleteFile(key);
    //             indexes.TryRemove(key, out _);
    //             changed = true;
    //         }
    //     }
    //
    //     if (changed)
    //         await this.WriteState().ConfigureAwait(false);
    // }

    // public async Task Clear()
    // {
    //     var indexes = await this.GetFileIndexes().ConfigureAwait(false);
    //     
    //     foreach (var value in indexes.Values)
    //     {
    //         await this
    //             .DeleteFile(value)
    //             .ConfigureAwait(false);
    //     }
    //     indexes.Clear();
    //     await this.WriteState().ConfigureAwait(false);
    // }

    
    protected virtual async Task WriteObject<T>(string fileName, T value)
    {
        var content = serializer.Serialize(value);
        await this.WriteFile(fileName, content).ConfigureAwait(false);
    }
    
    
    protected virtual async Task<T?> GetObject<T>(string fileName)
    {
        var content = await this.ReadFile(fileName).ConfigureAwait(false);
        if (String.IsNullOrWhiteSpace(content))
            return default;
        
        var obj = serializer.Deserialize<T>(content);
        return obj;
    }
    

    protected virtual async Task WriteState()
    {
        try
        {
            await this.semaphore.WaitAsync().ConfigureAwait(false);
            if (this._indexes != null)
                await this.WriteObject(IndexFile, this._indexes).ConfigureAwait(false);
        }
        finally
        {
            this.semaphore.Release();
        }   
    }


    protected const string IndexFile = "indexes.mediator";
    readonly SemaphoreSlim semaphore = new(1, 1);
    ConcurrentDictionary<string, ConcurrentDictionary<string, string>>? _indexes;
    protected async Task<ConcurrentDictionary<string, string>> GetIndexCategory(string category)
    {
        ConcurrentDictionary<string, string> catIndex = null!;
        try
        {
            await this.semaphore.WaitAsync().ConfigureAwait(false);
            this._indexes ??= await this
                .GetObject<ConcurrentDictionary<string, ConcurrentDictionary<string, string>>>(IndexFile)
                .ConfigureAwait(false);
            
            this._indexes ??= new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            catIndex = this._indexes.GetOrAdd(category, _ => new ConcurrentDictionary<string, string>());
        }
        finally
        {
            this.semaphore.Release();
        }
        return catIndex;
    }
    
    
    protected async Task<string> GetFileIndexer(string category, string key)
    {
        var dict = await this.GetIndexCategory(category).ConfigureAwait(false);
        var fileIndex = dict.GetOrAdd(key, _ => Guid.NewGuid().ToString());
        return fileIndex;
    }
}