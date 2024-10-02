namespace Shiny.Mediator.Infrastructure;


public class StorageService(IFileSystem fileSystem, ISerializerService serializer) : IStorageService
{
    public Task Set<T>(string key, T value)
    {
        var path = this.GetFilePath(key);
        var json = serializer.Serialize(value);
        File.WriteAllText(path, json); 
     
        return Task.CompletedTask;
    }
    

    public Task<T> Get<T>(string key)
    {
        T? returnValue = default;
        var path = this.GetFilePath(key);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            returnValue = serializer.Deserialize<T>(json)!;
        }
        
        return Task.FromResult(returnValue);
    }


    string GetFilePath(string key) => Path.Combine(fileSystem.CacheDirectory, $"{key}.mediator");
    
    public Task Remove(string key)
    {
        var fn = this.GetFilePath(key);
        if (File.Exists(fn))
            File.Delete(fn);

        return Task.CompletedTask;
    }
}