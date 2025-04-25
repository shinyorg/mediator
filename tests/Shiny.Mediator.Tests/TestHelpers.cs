using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public static class TestHelpers
{
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
    
    
    public static IServiceCollection AddConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        return services;
    }
}