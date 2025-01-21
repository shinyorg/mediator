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
        RequestContext<TRequest> context,
        RequestHandlerDelegate<TResult> next 
    )
    {
        var section = configuration.GetHandlerSection("UserErrorNotifications", context.Request!, context.RequestHandler);
        if (section == null)
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        try
        {
            logger.LogDebug("UserErrorNotifications Enabled - {Request}", context.Request);
            result = await next().ConfigureAwait(false);
        }
        catch (ValidateException)
        {
            throw; // this is a special case we let bubble through to prevent order of ops setup issues
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing pipeline for {Error}", typeof(TRequest).FullName);

            var title = String.Empty;
            var msg = String.Empty;
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
                title = locale.GetValue<string>("Title", "ERROR");
                msg = locale.GetValue<string>("Message", "An error occurred with your request");
                alerts.Display(title!, msg!);
            }

            context.UserErrorNotification(new UserErrorNotificationContext(ex, title!, msg!));
        }

        return result!;
    }
}