using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class StorageService(
    IFileSystem fileSystem, 
    ISerializerService serializer,
    ILogger<StorageService> logger
) : AbstractFileStorageService(serializer, logger)
{
    protected override Task WriteFile(string fileName, string content, CancellationToken cancellationToken)
    {
        var path = Path.Combine(fileSystem.AppDataDirectory, fileName);
        return File.WriteAllTextAsync(path, content, cancellationToken); 
    }

    
    protected override async Task<string?> ReadFile(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(fileSystem.AppDataDirectory, fileName);
        if (!File.Exists(path))
            return null;
        
        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return content;
    }
    

    protected override Task DeleteFile(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(fileSystem.AppDataDirectory, fileName);
        if (!File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}