using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Shiny.Mediator.Server.Infrastructure;


public class MediatorBackgroundService(
    IServiceProvider services,
    ILogger<MediatorBackgroundService> logger
) : IHostedService, IDisposable
{
    readonly Timer timer = new();
    async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            this.timer.Stop();
            logger.LogInformation("Clearing expired messages");
            using (var scope = services.CreateScope())
            {
                var store = scope.ServiceProvider.GetRequiredService<IDataStore>();
                await store.FailExpiredMessages(CancellationToken.None).ConfigureAwait(false);
                // TODO: cleanup
            }
            // TODO: cleanup expired messages
            // TODO: send out alerts if cluster issues
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing expired messages");
        }
        finally
        {
            this.timer.Start();
        }
    }

    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
        this.timer.Elapsed += this.TimerOnElapsed;
        this.timer.Start();
        return Task.CompletedTask;
    }
    

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.timer.Stop();
        return Task.CompletedTask;
    }
    
    
    public void Dispose()
    {
        this.timer.Dispose();
    }
}