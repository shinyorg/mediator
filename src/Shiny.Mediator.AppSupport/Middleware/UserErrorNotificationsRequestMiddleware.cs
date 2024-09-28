using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class UserErrorNotificationsRequestMiddleware<TRequest, TResult>(
    ILogger<UserErrorNotificationsRequestMiddleware<TRequest, TResult>> logger,
    IAlertDialogService alerts,
    IConfiguration configuration
) : IRequestMiddleware<TRequest, TResult>
{
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler, 
        CancellationToken cancellationToken
    )
    {
        var section = configuration.GetHandlerSection("UserErrorNotifications", request!, requestHandler);
        if (section == null)
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        try
        {
            logger.LogDebug("UserErrorNotifications Enabled - {Request}", request);
            result = await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing pipeline for {Error}", typeof(TRequest).FullName);
            
            var key = CultureInfo.CurrentUICulture.Name.ToLower();
            var locale = section.GetSection(key);
            if (!locale.Exists())
            {
                locale = section.GetSection("*");
                if (!locale.Exists())
                    logger.LogError("No locale found for {RequestType}", typeof(TRequest).FullName);
            }            
            
            if (locale.Exists())
            {
                var title = locale.GetValue<string>("Title", "ERROR");
                var msg = locale.GetValue<string>("Message", "An error occurred with your request");
                alerts.Display(title!, msg!);
            }
        }

        return result!;
    }
}