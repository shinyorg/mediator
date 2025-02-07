using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public static class AspNetExtensions
{
    public static string? IpAddress(this IHttpContextAccessor httpContextAccessor)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
            return null;
        
        var ip = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (String.IsNullOrEmpty(ip))
            ip = request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

        return ip;
    }


    /// <summary>
    /// Adds an ASPNET exception handler
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddAspNetExceptionHandler(this ShinyConfigurator cfg)
        => cfg.AddExceptionHandler<AspNetExceptionHandler>();
}