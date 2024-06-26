using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Tests;


public class OfflineAvailableRequestMiddlewareTests
{
    readonly MockConnectivity connectivity;
    readonly MockFileSystem fileSystem;
    readonly OfflineAvailableRequestMiddleware<OfflineRequest, long> middleware;
    readonly OfflineRequestHandler handler;
    
    public OfflineAvailableRequestMiddlewareTests()
    {
        this.handler = new();
        this.connectivity = new();
        this.fileSystem = new();

        this.middleware = new OfflineAvailableRequestMiddleware<OfflineRequest, long>(
            this.connectivity, 
            this.fileSystem
        );
    }
    
    
    [Fact]
    public async Task EndToEnd()
    {
        this.handler.ReturnValue = 99L;
        this.connectivity.IsAvailable = true;
        
        var func = () => this.middleware.Process(
            new OfflineRequest(),
            () => this.handler.Handle(new OfflineRequest(), CancellationToken.None),
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
    }
}


public record OfflineRequest : IRequest<long>;

public class OfflineRequestHandler : IRequestHandler<OfflineRequest, long>
{
    public bool WasHit { get; set; }
    public long ReturnValue { get; set; }
    
    [OfflineAvailable]
    public Task<long> Handle(OfflineRequest request, CancellationToken cancellationToken)
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