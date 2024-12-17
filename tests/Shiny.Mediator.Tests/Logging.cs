using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public static class Logging
{
    public static ILogger<T> CreateLogger<T>(ITestOutputHelper output)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(x => x.AddXUnit(output))
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger<T>();
        return logger;
    } 
}