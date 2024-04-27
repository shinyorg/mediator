using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;

public class MediatorServiceProviderFactory : IServiceProviderFactory<MediatorServiceProvider>
{
    public MediatorServiceProvider CreateBuilder(IServiceCollection services)
    {
        throw new NotImplementedException();
    }

    public IServiceProvider CreateServiceProvider(MediatorServiceProvider containerBuilder)
    {
        throw new NotImplementedException();
    }
}