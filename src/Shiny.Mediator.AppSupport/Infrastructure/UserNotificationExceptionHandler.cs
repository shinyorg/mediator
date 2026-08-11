using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Mediator exception handler that surfaces handler failures to the user as a localized alert dialog.
/// Title and message text are resolved from configuration under <c>UserErrorNotifications</c>, keyed by the
/// current UI culture (with <c>*</c> as a fallback locale). Child contexts and contexts without matching
/// configuration are ignored.
/// </summary>
public class UserNotificationExceptionHandler(
    IAlertDialogService alerts,
    ILogger<UserNotificationExceptionHandler>? logger = null,
    IConfiguration? configuration = null
) : IExceptionHandler
{
    /// <inheritdoc/>
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
            logger?.LogInformation("User Error Notifications not setup for: {RequestType}", msgType.FullName);
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
                logger?.LogInformation("No locale found for {RequestType}", msgType.FullName);
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
    
    /// <summary>
    /// Returns the culture code used to look up the localized notification text. Defaults to the current UI
    /// culture name in lowercase. Override to source the locale from app preferences instead.
    /// </summary>
    protected virtual string GetCultureCode() => CultureInfo.CurrentUICulture.Name.ToLower();
}
