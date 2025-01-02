using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.DapperRequests;

namespace Shiny.Mediator;


public static class ShinyConfiguratorExtensions
{
    public static ShinyConfigurator AddDapper<TConnection>(this ShinyConfigurator cfg, string connectionString)
        where TConnection : class, IDbConnection, new()
    {
        cfg.Services.AddSingleton<IConnectionProvider>(new SingleConnectionProvider<TConnection>(connectionString));
        return cfg.AddInfrastructure();
    }


    public static ShinyConfigurator AddDapper<TConnectionProvider>(this ShinyConfigurator cfg)
        where TConnectionProvider : class, IConnectionProvider
    {
        cfg.Services.AddSingleton<IConnectionProvider, TConnectionProvider>();
        return cfg.AddInfrastructure();
    }


    static ShinyConfigurator AddInfrastructure(this ShinyConfigurator cfg)
    {
        cfg.Services.AddScoped(typeof(IRequestHandler<,>), typeof(DapperQueryRequestHandler<>));
        cfg.Services.AddScoped(typeof(IRequestHandler<,>), typeof(DapperFirstQueryRequestHandler<>));
        cfg.Services.AddScoped(typeof(IRequestHandler<,>), typeof(DapperScalarQueryRequestHandler));
        return cfg;
    }
}