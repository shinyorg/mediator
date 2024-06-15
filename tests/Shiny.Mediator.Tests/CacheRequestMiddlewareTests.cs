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
    
    
    [Fact]
    public async Task OfflineOnlyTests()
    {
        this.handler.ReturnValue = 99L;
        this.connectivity.IsAvailable = true;
        
        var func = () => this.middleware.Process(
            new CacheAttribute { OnlyForOffline  = true, Storage = StoreType.Memory },
            new CacheRequest(),
            () => this.handler.Handle(new CacheRequest(), CancellationToken.None),
            this.handler,
            CancellationToken.None
        );

        var result = await func();
        result.Should().Be(99L, "Gate 1");
        this.handler.WasHit.Should().Be(true, "Gate 1");

        this.handler.WasHit = false;
        this.handler.ReturnValue = 88L;
        this.connectivity.IsAvailable = false;
        result = await func();
        
        this.handler.WasHit.Should().Be(false, "Gate 2");
        result.Should().Be(99L, "Gate 2");

        this.connectivity.IsAvailable = true;
        result = await func();
        this.handler.WasHit.Should().Be(true, "Gate 3");
        result.Should().Be(88L, "Gate 3");
    }
    
    
    [Fact]
    public async Task ExpiryTests()
    {
        this.handler.ReturnValue = 120L;
        
        var func = () => this.middleware.Process(
            new CacheAttribute { MaxAgeSeconds = 3, Storage = StoreType.Memory },
            new CacheRequest(),
            () => this.handler.Handle(new CacheRequest(), CancellationToken.None),
            this.handler,
            CancellationToken.None
        );

        // gate 1
        var result = await func();
        result.Should().Be(120L, "Gate 1");
        this.handler.WasHit.Should().Be(true, "Gate 1");

        // gate 2
        this.handler.WasHit = false;
        this.handler.ReturnValue = 130L;
        result = await func();
        this.handler.WasHit.Should().Be(false, "Gate 2");
        result.Should().Be(120L, "Gate 2");

        // gate 3
        await Task.Delay(3000);
        result = await func();
        this.handler.WasHit.Should().Be(true, "Gate 3");
        result.Should().Be(130L, "Gate 3");
    }
}


public record CacheRequest : IRequest<long>;

public class CacheRequestHandler : IRequestHandler<CacheRequest, long>
{
    public bool WasHit { get; set; }
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