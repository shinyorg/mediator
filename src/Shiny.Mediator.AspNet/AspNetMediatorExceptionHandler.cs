using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;

/// <summary>
/// Base class for mediator exception handlers running inside an ASP.NET request pipeline. Resolves the active
/// <see cref="HttpContext"/> from the request scope and forwards to the typed
/// <see cref="Handle(HttpContext, IMediatorContext, Exception)"/> hook for derived classes to implement.
/// </summary>
public abstract class AspNetMediatorExceptionHandler : IExceptionHandler
{
    /// <inheritdoc/>
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        var httpContextAccessor = context.ServiceScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        return this.Handle(httpContextAccessor.HttpContext!, context, exception);
    }


    /// <summary>
    /// Implements the exception-handling policy with access to the current <see cref="HttpContext"/>. Return
    /// <c>true</c> when the exception has been fully handled and the response has been written; <c>false</c>
    /// to let the mediator continue propagating the exception.
    /// </summary>
    protected abstract Task<bool> Handle(HttpContext httpContext, IMediatorContext context, Exception exception);
}