using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class UserNotificationExceptionHandler(
    ILogger<UserNotificationExceptionHandler> logger,
    IConfiguration configuration,
    IAlertDialogService alerts
) : IExceptionHandler
{
    public Task<bool> Handle(object message, object handler, Exception exception, MediatorContext context)
    {
        var msgType = message.GetType();
        var handled = false;
        var section = configuration.GetHandlerSection("UserErrorNotifications", message, handler);
        
        if (section != null)
        {
            logger.LogError(exception, "Error executing pipeline for {Error}", msgType.FullName);

            var title = String.Empty;
            var msg = String.Empty;
            var key = CultureInfo.CurrentUICulture.Name.ToLower();
            var locale = section.GetSection(key);

            if (!locale.Exists())
            {
                locale = section.GetSection("*");
                if (!locale.Exists())
                    logger.LogError("No locale found for {RequestType}", msgType.FullName);
            }

            if (locale.Exists())
            {
                title = locale.GetValue<string>("Title", "ERROR");
                msg = locale.GetValue<string>("Message", "An error occurred with your request");
                alerts.Display(title!, msg!);
                handled = true;
            }
        }
        return Task.FromResult(handled);
    }
}
