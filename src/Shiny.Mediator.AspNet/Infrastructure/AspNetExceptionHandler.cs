using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;

// what about OTEL?
public class AspNetExceptionHandler(
    IHttpContextAccessor httpAccessor,
    ILogger<AspNetExceptionHandler> logger
) : IExceptionHandler
{
    public async Task<bool> Handle(object message, object handler, Exception exception)
    {
        // TODO: include PII flag?
        // var claims = httpAccessor.HttpContext.User.Claims;

        var ip = httpAccessor.IpAddress();
        
        return true;
    }
}