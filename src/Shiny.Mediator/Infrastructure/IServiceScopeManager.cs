// namespace Shiny.Mediator.Infrastructure;
//
// // TODO: scheduled commands and fire-and-forget events always get new scopes
// // TODO: aspnet reuses request scope for everything else
// // TODO: maui does not create a scope for anything else
// public interface IServiceScopeManager
// {
//     // TODO: pass mediator context?  mediator context will need access to this thing anyhow
//     MediatorServiceScope CreateRequestScope();
//     MediatorServiceScope CreateCommandScope(bool forScheduled);
//     MediatorServiceScope CreateEventScope(bool forFireAndForget);
// }
//
// public class MediatorServiceScope(IServiceProvider services, Action? onDispose = null) : IDisposable
// {
//     public IServiceProvider Services => services;
//     public void Dispose() => onDispose?.Invoke();
// }
//
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Shiny.Mediator.Infrastructure.Impl;
//
//
// public class DefaultServiceScopeManager(IServiceProvider services) : IServiceScopeManager
// {
//     static bool aspNet = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
//
//
//     protected virtual bool IsAspNet() => aspNet;
//     
//     public MediatorServiceScope CreateRequestScope()
//     {
//         throw new NotImplementedException();
//     }
//
//
//     public MediatorServiceScope CreateCommandScope(bool forScheduled)
//     {
//         if (forScheduled)
//             return this.CreateNewScope();
//         
//         // if aspnet, use existing scope, otherwise new scope
//         if (this.IsAspNet())
//             return new MediatorServiceScope(services);
//         
//         var newScope = services.CreateScope();
//         return new MediatorServiceScope(newScope.ServiceProvider, () => newScope.Dispose());
//     }
//
//
//     public MediatorServiceScope CreateEventScope(bool forFireAndForget)
//     {
//         if (forFireAndForget)
//             return this.CreateNewScope();
//
//         // TODO: use existing scope?
//         return new MediatorServiceScope(services);
//     }
//     
//     
//     protected virtual MediatorServiceScope CreateNewScope()
//     {
//         var scope = services.CreateScope();
//         return new MediatorServiceScope(scope.ServiceProvider, () => scope.Dispose());
//     }
// }
//
// using Microsoft.AspNetCore.Http;
//
// namespace Shiny.Mediator.Infrastructure;
//
// TODO: this must be scoped but IMediator is singleton
// public class AspNetServiceScopeManager(IHttpContextAccessor http) : IServiceScopeManager
// {
//     public MediatorServiceScope CreateRequestScope()
//     {
//         // http.HttpContext?.RequestServices
//         throw new NotImplementedException();
//     }
//
//
//     public MediatorServiceScope CreateCommandScope(bool forScheduled)
//     {
//         throw new NotImplementedException();
//     }
//
//
//     public MediatorServiceScope CreateEventScope(bool forFireAndForget)
//     {
//         throw new NotImplementedException();
//     }
// }