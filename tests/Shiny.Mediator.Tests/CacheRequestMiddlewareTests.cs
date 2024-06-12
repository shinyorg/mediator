using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Tests;

public class CacheRequestMiddlewareTests
{
    readonly MockConnectivity connectivity;
    readonly MockFileSystem fileSystem;
    readonly CacheRequestMiddleware<CacheRequest, long> middleware;
    readonly CacheRequestHandler handler;
    
    public CacheRequestMiddlewareTests()
    {
        this.handler = new();
        this.connectivity = new();
        this.fileSystem = new();
        this.middleware = new CacheRequestMiddleware<CacheRequest, long>(this.connectivity, this.fileSystem);
    }
    
    // online with offlineonly
    // offline with offlineonly
    // offline with expired 
    // offline with no cache
    // test where it should not be caching, double request handler - cache on one method, not on other
    // custom cache key
    [Fact]
    public async Task Offline_NoCache()
    {
        this.connectivity.IsAvailable = false;
        var result = await this.middleware.Process(
            new CacheRequest(),
            () => Task.FromResult(-1L),
            this.handler,
            CancellationToken.None
        );
        result.Should().Be(0);
        this.handler.WasHit.Should().BeFalse();
    }

    [Fact]
    public async Task Online_OnlyForOffline()
    {
        this.connectivity.IsAvailable = true;
        this.handler.ReturnValue = 100L;
        
        // TODO: make sure it has a cache value already
        var result = await this.middleware.Process(
            new CacheAttribute
            {
                OnlyForOffline = true,
                Storage = StoreType.Memory
            },
            new CacheRequest(),
            () => Task.FromResult(-1L),
            this.handler,
            CancellationToken.None
        );
        result.Should().Be(0);
        this.handler.WasHit.Should().BeFalse();
    }
    
    
    [Fact]
    public async Task EndToEnd()
    {
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
    public bool WasHit { get; private set; }
    public long ReturnValue { get; set; }
    
    [Cache(MaxAgeSeconds = 5, Storage = StoreType.Memory, OnlyForOffline = false)]
    public Task<long> Handle(CacheRequest request, CancellationToken cancellationToken)
    {
        this.WasHit = true;
        return Task.FromResult(this.ReturnValue);
    }
}
public class MockConnectivity : IConnectivity
{
    public bool IsAvailable
    {
        get => this.NetworkAccess == NetworkAccess.Internet;
        set => this.NetworkAccess = value ? NetworkAccess.Internet : NetworkAccess.None;
    }
    
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