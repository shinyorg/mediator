using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Middleware;


public class UserExceptionRequestMiddlewareConfig
{
    public bool ShowFullException { get; set; }
    public string ErrorMessage { get; set; } = "We're sorry. An error has occurred";
    public string ErrorTitle { get; set; } = "Error";
    public string ErrorConfirm { get; set; } = "OK";
}

public class UserExceptionRequestMiddleware<TRequest, TResult>(ILogger<TRequest> logger, UserExceptionRequestMiddlewareConfig config) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(TRequest request, RequestHandlerDelegate<TResult> next, IRequestHandler<TRequest, TResult> requestHandler, CancellationToken cancellationToken)
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