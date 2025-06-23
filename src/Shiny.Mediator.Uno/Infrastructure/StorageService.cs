using System.IO;
using Windows.Storage;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class StorageService(
    ISerializerService serializer,
    ILogger<StorageService> logger
) : AbstractFileStorageService(serializer, logger)
{
    protected override async Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
    {
        var local = ApplicationData.Current.LocalFolder;
        var file = await local.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
        await FileIO.WriteTextAsync(file, content);
    }

    
    protected override async Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
    {
        var local = ApplicationData.Current.LocalFolder;
        var fn = Path.Combine(local.Path, fileName);
        if (!File.Exists(fn))
            return null;

        var file = await local.GetFileAsync(fn);
        var content = await FileIO.ReadTextAsync(file);
        return content;
    }
    

    protected override async Task DeleteFile(string fileName, CancellationToken cancellationToken)
    {
        var local = ApplicationData.Current.LocalFolder;
        var fn = Path.Combine(local.Path, fileName);
        if (File.Exists(fn))
        {
            var file = await local.GetFileAsync(fileName);
            await file.DeleteAsync();
        }
    }
}