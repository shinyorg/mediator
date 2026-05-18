using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


/// <summary>
/// ASP.NET-specific mediator builder and HTTP context extensions, including caller IP resolution and JSON
/// validation exception handling.
/// </summary>
public static class AspNetExtensions
{
    /// <summary>
    /// Resolves the calling client's IP address from the current <see cref="HttpContext"/>. Returns <c>null</c>
    /// when no HTTP context is active.
    /// </summary>
    public static string? IpAddress(this IHttpContextAccessor httpContextAccessor)
        => httpContextAccessor.HttpContext?.IpAddress();


    /// <summary>
    /// Resolves the calling client's IP address, preferring the first value of the <c>X-Forwarded-For</c>
    /// header (when set by a proxy) and falling back to the IPv4-mapped remote endpoint.
    /// </summary>
    public static string? IpAddress(this HttpContext httpContext)
    {
        var request = httpContext?.Request;
        if (request == null)
            return null;

        var ip = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (String.IsNullOrEmpty(ip))
            ip = request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

        return ip;
    }


    /// <summary>
    /// Registers <see cref="ValidationJsonExceptionHandler"/> so <see cref="ValidateException"/> thrown by the
    /// mediator validation middleware is converted into an HTTP 400 response with a JSON body describing the
    /// validation result.
    /// </summary>
    public static ShinyMediatorBuilder AddJsonValidationExceptionHandler(this ShinyMediatorBuilder builder)
    {
        builder.AddExceptionHandler<ValidationJsonExceptionHandler>(ServiceLifetime.Scoped);
        return builder;
    }
}