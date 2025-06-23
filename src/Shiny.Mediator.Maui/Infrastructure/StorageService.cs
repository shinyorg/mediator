using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class StorageService(
    IFileSystem fileSystem, 
    ISerializerService serializer,
    ILogger<StorageService> logger
) : AbstractFileStorageService(serializer, logger)
{
    protected override Task WriteFile(string fileName, string content)
    {
        var path = Path.Combine(fileSystem.AppDataDirectory, fileName);
        return File.WriteAllTextAsync(path, content); 
    }

    
    protected override Task<string?> ReadFile(string fileName)
    {
        var path = Path.Combine(fileSystem.AppDataDirectory, fileName);
        if (!File.Exists(path))
            return Task.FromResult<string?>(null);
        
        return File.ReadAllTextAsync(path);
    }
    

    protected override Task DeleteFile(string fileName)
    {
        var path = Path.Combine(fileSystem.AppDataDirectory, fileName);
        if (!File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
