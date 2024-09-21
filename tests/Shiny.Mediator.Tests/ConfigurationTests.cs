using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class ConfigurationTests(ITestOutputHelper output)
{
    [Fact]
    public void AddConfiguration()
    {
        // TODO: ORDER: config, attribute contract, attribute handler
        // TODO: config order: full type, exact namespace, sub namespace, *
        var dict = new Dictionary<string, object>();
        var config = new ConfigurationManager();
        // config.AddConfiguration(new MemoryConfigurationProvider(new MemoryConfigurationSource().InitialData));
        // config.Get("key1").Should().Be("value1");
    }
}