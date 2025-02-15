using System.IO;
using Windows.Storage;

namespace Shiny.Mediator.Infrastructure;


public class StorageService(ISerializerService serializer) : IStorageService
{
    public async Task Set<T>(string key, T value)
    {
        var local = ApplicationData.Current.LocalFolder;
        var json = serializer.Serialize(value);
        var file = await local.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(file, json);
    }
    

    public async Task<T?> Get<T>(string key)
    {
        var local = ApplicationData.Current.LocalFolder;
        var fn = Path.Combine(local.Path, key);
        if (!File.Exists(fn))
            return default;
        
        var file = await local.GetFileAsync(key);
        var json = await FileIO.ReadTextAsync(file);
        if (String.IsNullOrWhiteSpace(json))
            return default;
        
        var returnValue = serializer.Deserialize<T>(json);
        return returnValue;
    }

    
    public  async Task Remove(string key)
    {
        var local = ApplicationData.Current.LocalFolder;
        var file = await local.GetFileAsync(key);
        if (file != null)
            await file.DeleteAsync();
    }

    public Task RemoveByPrefix(string prefix) => this.DeleteBy(prefix);
    public Task Clear() => this.DeleteBy(null);


    async Task DeleteBy(string? startsWith)
    {
        var local = ApplicationData.Current.LocalFolder;
        var files = await local.GetFilesAsync();
        foreach (var file in files)
        {
            var ext = System.IO.Path.GetExtension(file.Name);
            if (ext.Equals(startsWith, StringComparison.OrdinalIgnoreCase))
            {
                if (startsWith == null || file.Name.StartsWith(startsWith))
                    await file.DeleteAsync();
            }
            await file.DeleteAsync();
        }
    }
}