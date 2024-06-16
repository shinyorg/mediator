using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class UserExceptionRequestMiddleware<TRequest, TResult>(ILogger<TRequest> logger, UserExceptionRequestMiddlewareConfig config) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(TRequest request, RequestHandlerDelegate<TResult> next, IRequestHandler requestHandler, CancellationToken cancellationToken)
    {
        var result = default(TResult);
        try
        {
            result = await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error executing pipeline for {typeof(TRequest).FullName}");
            try
            {
                var msg = config.ShowFullException ? ex.ToString() : config.ErrorMessage;
                await Application.Current!.MainPage!.DisplayAlert(config.ErrorTitle, msg, config.ErrorConfirm);
            }
            catch
            {
            }
        }

        return result!;
    }
}