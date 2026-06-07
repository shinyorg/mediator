using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Impl;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public static class TestHelpers
{
    /// <summary>
    /// Builds a <see cref="Shiny.ISerializer"/> whose chain includes the reflection-based
    /// <see cref="DefaultJsonTypeInfoResolver"/>. Tests use this so they can serialize ad-hoc and
    /// anonymous types without declaring a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
    /// Production code goes through the AOT-safe chain registered via <c>[ShinyJsonContext]</c> instead.
    /// </summary>
    public static Shiny.ISerializer CreateTestSerializer()
    {
        var inner = new DefaultJsonSerializer();
        inner.Options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
        return inner;
    }



    public static ILogger<T> CreateLogger<T>(ITestOutputHelper output)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(x =>
            {
                x.AddXUnit(output);
                x.SetMinimumLevel(LogLevel.Debug);
            })
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger<T>();
        return logger;
    }

    
    public static IServiceCollection AddXUnitLogging(this IServiceCollection services, ITestOutputHelper output)
    {
        services.AddLogging(x =>
        {
            x.AddXUnit(output);
            x.SetMinimumLevel(LogLevel.Debug);
        });
        return services;
    }
    
    
    public static IServiceCollection AddConfiguration(this IServiceCollection services, Action<ConfigurationManager>? configure = null)
    {
        var config = new ConfigurationManager();
        configure?.Invoke(config);
        services.AddSingleton<IConfiguration>(config);
        return services;
    }
}