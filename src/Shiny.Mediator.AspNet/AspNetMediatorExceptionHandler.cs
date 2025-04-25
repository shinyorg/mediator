using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;

public abstract class AspNetMediatorExceptionHandler : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        var httpContextAccessor = context.ServiceScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        return this.Handle(httpContextAccessor.HttpContext!, context, exception);
    }


    protected abstract Task<bool> Handle(HttpContext httpContext, IMediatorContext context, Exception exception);
}