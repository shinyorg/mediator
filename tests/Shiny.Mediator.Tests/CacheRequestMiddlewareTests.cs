using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Tests;

public class CacheRequestMiddlewareTests
{
    [Fact]
    public async Task EndToEnd()
    {
        var conn = new MockConnectivity();
        var fs = new MockFileSystem();

        var handler = new CacheRequestHandler();
        var middleware = new CacheRequestMiddleware<CacheRequest, CacheResult>(conn, fs);

        // TODO: test with ICacheItem
        var request = new CacheRequest();

        var result1 = await middleware.Process(
            request,
            () => Task.FromResult(
                new CacheResult(DateTimeOffset.UtcNow.Ticks)
            ), 
            handler,
            CancellationToken.None
        );
        await Task.Delay(2000);
        var result2 = await middleware.Process(
            request,
            () => Task.FromResult(
                new CacheResult(DateTimeOffset.UtcNow.Ticks)
            ), 
            handler,
            CancellationToken.None
        );
        
        // if cached
        result1.Ticks.Should().Be(result2.Ticks);
    }
}


public record CacheRequest : IRequest<CacheResult>;
public record CacheResult(long Ticks);

[Cache(MaxAgeSeconds = 5, Storage = StoreType.Memory, OnlyForOffline = false)]
public class CacheRequestHandler : IRequestHandler<CacheRequest, CacheResult>
{
    public Task<CacheResult> Handle(CacheRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new CacheResult(DateTimeOffset.UtcNow.Ticks));
}
public class MockConnectivity : IConnectivity
{
    public IEnumerable<ConnectionProfile> ConnectionProfiles { get; set; }// = ConnectionProfile.WiFi;
    public NetworkAccess NetworkAccess { get; set; }
    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
}
public class MockFileSystem : IFileSystem
{
    public Task<Stream> OpenAppPackageFileAsync(string filename)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AppPackageFileExistsAsync(string filename)
    {
        throw new NotImplementedException();
    }

    public string CacheDirectory { get; } = ".";
    public string AppDataDirectory { get; } = ".";
}