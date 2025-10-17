using System.Diagnostics.CodeAnalysis;
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
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Get will not be trimmed")]
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        // If this is a child context, we do not handle it with user notifications
        if (context.Parent != null)
            return Task.FromResult(false);
        
        var msgType = context.Message.GetType();
        var handled = false;
        var section = configuration.GetHandlerSection(
            "UserErrorNotifications", 
            context.Message, 
            context.MessageHandler
        );

        if (section == null)
        {
            logger.LogInformation("User Error Notifications not setup for: {RequestType}", msgType.FullName);
        }
        else
        {
            var title = String.Empty;
            var msg = String.Empty;
            var cultureKey = this.GetCultureCode().ToLower();
            var locale = section.GetSection(cultureKey);

            if (!locale.Exists())
                locale = section.GetSection("*");

            if (!locale.Exists())
            {
                logger.LogInformation("No locale found for {RequestType}", msgType.FullName);
            }
            else
            {
                title = locale.GetValue<string>("Title", "ERROR");
                msg = locale.GetValue<string>("Message", "An error occurred with your request");
                alerts.Display(title!, msg!);
                handled = true;
            }
        }
        return Task.FromResult(handled);
    }
    
    protected virtual string GetCultureCode() => CultureInfo.CurrentUICulture.Name.ToLower();
}
