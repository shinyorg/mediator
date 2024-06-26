namespace Shiny.Mediator.Middleware;


public class OfflineAvailableFlushRequestHandler(IFileSystem fileSystem) : IRequestHandler<OfflineAvailableFlushRequest>
{
    public Task Handle(OfflineAvailableFlushRequest request, CancellationToken cancellationToken)
    {
        foreach (var path in Directory.GetFiles(fileSystem.CacheDirectory, "*.off"))
            File.Delete(path);

        return Task.CompletedTask;
    }
}