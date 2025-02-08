using Windows.Storage;

namespace Shiny.Mediator.Infrastructure;


public class StorageService(ISerializerService serializer) : IStorageService
{
    static readonly StorageFolder local = ApplicationData.Current.LocalFolder;
    
    public async Task Set<T>(string key, T value)
    {
        var json = serializer.Serialize(value);
        var file = await local.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(file, json);
    }
    

    public async Task<T?> Get<T>(string key)
    {
        var file = await local.GetFileAsync(key);
        if (file == null)
            return default;

        var json = await FileIO.ReadTextAsync(file);
        if (String.IsNullOrWhiteSpace(json))
            return default;
        
        var returnValue = serializer.Deserialize<T>(json);
        return returnValue;
    }

    
    public  async Task Remove(string key)
    {
        var file = await local.GetFileAsync(key);
        if (file != null)
            await file.DeleteAsync();
    }

    public Task RemoveByPrefix(string prefix) => this.DeleteBy(prefix);
    public Task Clear() => this.DeleteBy(null);


    async Task DeleteBy(string? startsWith)
    {
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