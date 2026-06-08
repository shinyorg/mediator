using Microsoft.AspNetCore.Http;

namespace Shiny.Mediator.Infrastructure;

/// <summary>
/// ASP.NET mediator exception handler that converts <see cref="ValidateException"/> into an HTTP 400 response
/// with a JSON body containing the validation result. Other exceptions are not handled.
/// </summary>
public class ValidationJsonExceptionHandler(Shiny.ISerializer serializer) : AspNetMediatorExceptionHandler
{
    /// <inheritdoc/>
    protected override async Task<bool> Handle(HttpContext httpContext, IMediatorContext context, Exception exception)
    {
        if (exception is ValidateException ex)
        {
            // Route through the shared Shiny.ISerializer so the response body uses the chain that
            // AspNetJsonResolver auto-registers ValidateResult into — works under the strict
            // (AOT-clean) chain without callers declaring ValidateResult in their own context.
            var json = serializer.Serialize(ex.Result);
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            await httpContext
                .Response
                .WriteAsync(json, CancellationToken.None)
                .ConfigureAwait(false);

            return true;
        }
        return false;
    }
}
