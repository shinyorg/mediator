using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class ConfigurationTests(ITestOutputHelper output)
{
    [Fact]
    public void AddConfiguration()
    {
        // var config = new ConfigurationManager();
        // config.AddConfiguration(new MemoryConfigurationProvider(new MemoryConfigurationSource().InitialData));
        // config.Get("key1").Should().Be("value1");
    }
}