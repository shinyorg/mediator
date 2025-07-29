using DeviceRunners.UITesting;
using DeviceRunners.VisualRunners;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Maui.Tests;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .ConfigureUITesting()
            .UseVisualTestRunner(conf => conf
                    .AddXunit()
                    .AddConsoleResultChannel()
                    .AddTestAssembly(typeof(MauiProgram).Assembly)
#if MODE_NON_INTERACTIVE_VISUAL
				.EnableAutoStart(true)
				.AddTcpResultChannel(new TcpResultChannelOptions
				{
					HostNames = ["localhost", "10.0.2.2"],
					Port = 16384,
					Formatter = new TextResultChannelFormatter(),
					Required = false,
					Retries = 3,
					RetryTimeout = TimeSpan.FromSeconds(5),
					Timeout = TimeSpan.FromSeconds(30)
				})
#endif
            );

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}