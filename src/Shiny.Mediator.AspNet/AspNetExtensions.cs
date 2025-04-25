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
    /// If the mediator validation middleware throws a validation exception, this will translate it to JSON output
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddJsonValidationExceptionHandler(this ShinyMediatorBuilder builder)
    {
        builder.AddExceptionHandler<ValidationJsonExceptionHandler>(ServiceLifetime.Scoped);
        return builder;
    }
}