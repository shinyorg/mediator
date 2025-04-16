using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Shiny.Mediator.Infrastructure;

public class ValidationJsonExceptionHandler : AspNetMediatorExceptionHandler
{
    protected override async Task<bool> Handle(HttpContext httpContext, IMediatorContext context, Exception exception)
    {
        if (exception is ValidateException ex)
        {
            var json = JsonSerializer.Serialize(ex.Result);
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