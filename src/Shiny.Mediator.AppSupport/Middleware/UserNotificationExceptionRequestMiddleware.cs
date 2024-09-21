using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class UserExceptionRequestMiddleware<TRequest, TResult>(
    ILogger<TRequest> logger,
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
        var section = configuration.GetHandlerSection("UserNotify", request!, requestHandler);
        if (section == null)
            return await next().ConfigureAwait(false);

        
        var key = CultureInfo.CurrentUICulture.Name;
        // TODO: if key for locale not found, default somehow?
        
        var result = default(TResult);
        try
        {
            result = await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error executing pipeline for {typeof(TRequest).FullName}");
        //     try
        //     {
        //         var msg = config.ShowFullException ? ex.ToString() : attribute.ErrorMessage ?? config.ErrorMessage;
        //         await Application.Current!.MainPage!.DisplayAlert(attribute.ErrorTitle ?? config.ErrorTitle, msg, config.ErrorConfirm);
        //     }
        //     catch
        //     {
        //     }
        }

        return result!;
    }
}