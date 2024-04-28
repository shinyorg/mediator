using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;

public class MediatorServiceProviderFactory : IServiceProviderFactory<MediatorServiceProvider>
{
    public MediatorServiceProvider CreateBuilder(IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var scope = new InternalGlobalScope(sp);
        return new MediatorServiceProvider(scope, false);
    }

    public IServiceProvider CreateServiceProvider(MediatorServiceProvider containerBuilder)
    {
        var scope = containerBuilder.CreateScope();
        return new MediatorServiceProvider(scope, true);
    }
}

public class InternalGlobalScope(IServiceProvider services) : IServiceScope
{
    public IServiceProvider ServiceProvider => services;
    public void Dispose() => (services as IDisposable)?.Dispose();
}