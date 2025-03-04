using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator.Tests;


public class UtilsTests
{
    [Theory]
    [InlineData("Test")]
    public void Configuration_ProperOrdering(string name)
    {
        // TODO: ORDER: config, attribute contract, attribute handler
        // TODO: config order: full type, exact namespace, sub namespace, *
        var config = new ConfigurationManager();
        
    }


    [Fact]
    public void GetHandlerHandleMethodAttribute_Request_ProperMethod()
    {
        
    }


    [Fact]
    public void GetHandlerHandleMethodAttribute_Event_ProperMethod()
    {
        
    }


    [Fact]
    public void GetHandlerHandleMethodAttribute_Command_ProperMethod()
    {
        
    }

    ConfigurationManager SetupConfig(params (string Key, bool Enabled)[] keys)
    {
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(keys.ToDictionary(x => x.Key, x => x.Enabled.ToString())!);
        return config;
    }
}