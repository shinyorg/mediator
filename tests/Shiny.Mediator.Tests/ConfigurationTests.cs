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

    }


    ConfigurationManager SetupConfig(params (string Key, bool Enabled)[] keys)
    {
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(keys.ToDictionary(x => x.Key, x => x.Enabled.ToString())!);
        return config;
    }
}