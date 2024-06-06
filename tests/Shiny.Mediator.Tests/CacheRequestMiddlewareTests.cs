using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator.Tests;

public class CacheRequestMiddlewareTests
{
    [Fact]
    public async Task EndToEnd()
    {
        var conn = new MockConnectivity();
        var fs = new MockFileSystem();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Cache:Shiny.Mediator.Tests.CacheRequest"] = ""
            })
            .Build();

        var handler = new CacheRequestHandler();
        var middleware = new CacheRequestMiddleware<CacheRequest, CacheResult>(configuration, conn, fs);

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