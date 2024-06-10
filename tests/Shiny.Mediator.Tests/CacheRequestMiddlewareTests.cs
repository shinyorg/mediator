using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Tests;

public class CacheRequestMiddlewareTests
{
    // online with offlineonly
    // offline with offlineonly
    // offline with expired 
    // offline with no cache
    // test where it should not be caching, double request handler - cache on one method, not on other
    // custom cache key
    [Fact]
    public async Task EndToEnd()
    {
        var conn = new MockConnectivity();
        var fs = new MockFileSystem();

        var handler = new CacheRequestHandler();
        var middleware = new CacheRequestMiddleware<CacheRequest, long>(conn, fs);

        // TODO: test with ICacheItem
        var request = new CacheRequest();

        var result1 = await middleware.Process(
            request,
            () => Task.FromResult(DateTimeOffset.UtcNow.Ticks),
            handler,
            CancellationToken.None
        );
        await Task.Delay(2000);
        var result2 = await middleware.Process(
            request,
            () => Task.FromResult(DateTimeOffset.UtcNow.Ticks),
            handler,
            CancellationToken.None
        );
        
        // if cached
        result1.Should().Be(result2);
    }
}


public record CacheRequest : IRequest<long>;

public class CacheRequestHandler : IRequestHandler<CacheRequest, long>
{
    [Cache(MaxAgeSeconds = 5, Storage = StoreType.Memory, OnlyForOffline = false)]
    public Task<long> Handle(CacheRequest request, CancellationToken cancellationToken)
        => Task.FromResult(DateTimeOffset.UtcNow.Ticks);
}
public class MockConnectivity : IConnectivity
{
    public IEnumerable<ConnectionProfile> ConnectionProfiles { get; set; }// = ConnectionProfile.WiFi;
    public NetworkAccess NetworkAccess { get; set; } = NetworkAccess.Internet;
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