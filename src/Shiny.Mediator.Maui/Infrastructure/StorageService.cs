using System.Text.Json;

namespace Shiny.Mediator.Infrastructure;


public class StorageService(IFileSystem fileSystem) : IStorageService
{
    Dictionary<string, string> keys = null!;
    
    
    public virtual Task Store(object request, object result)
    {
        var path = this.GetFilePath(request, true);
        var json = JsonSerializer.Serialize(result);
        File.WriteAllText(path, json); 
     
        return Task.CompletedTask;
    }
    

    public virtual Task<TResult?> Get<TResult>(object request)
    {
        TResult? returnValue = default;
        var path = this.GetFilePath(request, false);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var obj = JsonSerializer.Deserialize<TResult>(json)!;
            returnValue = obj;
        }
        
        return Task.FromResult(returnValue);
    }
    
    
    public Task Clear()
    {
        lock (this.keys)
            this.keys.Clear();
        
        Directory.GetFiles(fileSystem.CacheDirectory, "*.mediator").ToList().ForEach(File.Delete);
        Directory.GetFiles(fileSystem.AppDataDirectory, "*.mediator").ToList().ForEach(File.Delete);

        return Task.CompletedTask;
    }

    
    protected virtual string GetFilePath(object request, bool createIfNotExists)
    {
        var key = this.GetPersistentStoreKey(request, createIfNotExists);
        var path = Path.Combine(fileSystem.CacheDirectory, $"{key}.mediator");
        return path;
    }

    
    protected virtual string GetStoreKeyFromRequest(object request)
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
        if (this.keys.ContainsKey(key))
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
    
    
    bool initialized = false;
    protected void EnsureKeyLoad()
    {
        if (this.initialized)
            return;

        var storePath = this.KeyStorePath;
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


    protected void PersistKeyStore()
    {
        var json = JsonSerializer.Serialize(this.keys);
        File.WriteAllText(this.KeyStorePath, json);
    }


    protected string KeyStorePath => Path.Combine(fileSystem.AppDataDirectory, "keys.mediator");
}