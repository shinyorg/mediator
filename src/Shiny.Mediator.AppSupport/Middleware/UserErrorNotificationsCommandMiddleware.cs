using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class UserErrorNotificationsCommandMiddleware<TCommand>(
    ILogger<UserErrorNotificationsCommandMiddleware<TCommand>> logger,
    IAlertDialogService alerts,
    IConfiguration configuration
) : ICommandMiddleware<TCommand> where TCommand : ICommand
{
    public async Task Process(CommandContext<TCommand> context, CommandHandlerDelegate next, CancellationToken cancellationToken)
    {
        var section = configuration.GetHandlerSection("UserErrorNotifications", context.Command!, context.Handler);
        if (section == null)
        {
            await next().ConfigureAwait(false);
            return;
        }

        try
        {
            logger.LogDebug("UserErrorNotifications Enabled - {Command}", context.Command);
            await next().ConfigureAwait(false);
        }
        catch (ValidateException)
        {
            throw; // this is a special case we let bubble through to prevent order of ops setup issues
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing pipeline for {Error}", typeof(TCommand).FullName);

            var title = String.Empty;
            var msg = String.Empty;
            var key = CultureInfo.CurrentUICulture.Name.ToLower();
            var locale = section.GetSection(key);

            if (!locale.Exists())
            {
                locale = section.GetSection("*");
                if (!locale.Exists())
                    logger.LogError("No locale found for {RequestType}", typeof(TCommand).FullName);
            }            
            
            if (locale.Exists())
            {
                title = locale.GetValue<string>("Title", "ERROR");
                msg = locale.GetValue<string>("Message", "An error occurred with your request");
                alerts.Display(title!, msg!);
            }

            context.UserErrorNotification(new UserErrorNotificationContext(ex, title!, msg!));
        }
    }
}