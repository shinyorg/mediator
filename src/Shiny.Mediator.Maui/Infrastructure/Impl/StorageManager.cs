using System.Text.Json;

namespace Shiny.Mediator.Infrastructure.Impl;


// TODO: can the request keys get "dirty" - chances are yes
public class StorageManager(IFileSystem fileSystem) : IStorageManager
{
    readonly Dictionary<string, object> memCache = new();
    Dictionary<string, string> keys = null!;
    
    
    public void Store(object request, object result, bool isPersistent)
    {
        if (isPersistent)
        {
            var path = this.GetFilePath(request, true);
            var json = JsonSerializer.Serialize(result);
            File.WriteAllText(path, json); 
        }
        else
        {
            var key = this.GetStoreKeyFromRequest(request);
            lock (this.memCache)
                this.memCache[key] = result!;
        }
    }
    

    public TResult? Get<TResult>(object request, bool isPersistent)
    {
        TResult? returnValue = default;

        if (isPersistent)
        {
            var path = this.GetFilePath(request, false);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var obj = JsonSerializer.Deserialize<TResult>(json)!;
                returnValue = obj;
            }
        }
        else 
        {
            var key = this.GetStoreKeyFromRequest(request);
            lock (this.memCache)
            {
                if (this.memCache.ContainsKey(key))
                {
                    var item = this.memCache[key];
                    returnValue = (TResult)item;
                }
            }
        }
        return returnValue;
    }

    public void ClearAll()
    {
        lock (this.memCache)
            this.memCache.Clear();

        lock (this.keys)
            this.keys.Clear();
        
        Directory.GetFiles(fileSystem.CacheDirectory, "*.mediator").ToList().ForEach(File.Delete);
        Directory.GetFiles(fileSystem.AppDataDirectory, "*.mediator").ToList().ForEach(File.Delete);
    }


    string GetStoreKeyFromRequest(object request)
    {
        if (request is IRequestKey keyProvider)
            return keyProvider.GetKey();
        
        var t = request.GetType();
        var key = $"{t.Namespace}_{t.Name}";

        return key;
    }
    
    
    protected virtual string GetPersistentStoreKey(object request, bool createIfNotExists)
    {
        var key = this.GetStoreKeyFromRequest(request);
        this.EnsureKeyLoad();
        if (!this.keys.ContainsKey(key))
        {
            key = this.keys[key];
        }
        else if (createIfNotExists)
        {
            var newKey = Guid.NewGuid().ToString();
            this.keys.Add(key, newKey);
            key = newKey;

            this.PersistKeyStore();
        }
        
        return key;
    }


    protected virtual string GetFilePath(object request, bool createIfNotExists)
    {
        var key = this.GetPersistentStoreKey(request, createIfNotExists);
        var path = Path.Combine(fileSystem.CacheDirectory, $"{key}.mediator");
        return path;
    }
    

    bool initialized = false;
    void EnsureKeyLoad()
    {
        if (this.initialized)
            return;

        var storePath = Path.Combine(fileSystem.AppDataDirectory, "keys.mediator");
        if (File.Exists(storePath))
        {
            var json = File.ReadAllText(storePath);
            this.keys = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }
        else
        {
            this.keys = new();
        }
        this.initialized = true;
    }


    void PersistKeyStore()
    {
        var storePath = Path.Combine(fileSystem.AppDataDirectory, "keys.mediator");
        var json = JsonSerializer.Serialize(this.keys);
        File.WriteAllText(storePath, json);
    }
}