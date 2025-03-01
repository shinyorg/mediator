using System.Data;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.DapperRequests;
using Shiny.Mediator.DapperRequests.Handlers;

namespace Shiny.Mediator;


public static class ShinyConfiguratorExtensions
{
    public static ShinyMediatorBuilder AddDapper<TConnection>(this ShinyMediatorBuilder cfg, string connectionString)
        where TConnection : class, IDbConnection, new()
    {
        cfg.Services.AddSingleton<IConnectionProvider>(new SingleConnectionProvider<TConnection>(connectionString));
        return cfg.AddInfrastructure();
    }


    public static ShinyMediatorBuilder AddDapper<TConnectionProvider>(this ShinyMediatorBuilder cfg)
        where TConnectionProvider : class, IConnectionProvider
    {
        cfg.Services.AddSingleton<IConnectionProvider, TConnectionProvider>();
        return cfg.AddInfrastructure();
    }


    static ShinyMediatorBuilder AddInfrastructure(this ShinyMediatorBuilder cfg)
    {
        // typeof(DapperFirstQueryRequestHandler<>)
        //     .GetInterfaces()
        //     .ToList()
        //     .ForEach(x => cfg.Services.AddScoped(x, typeof(DapperFirstQueryRequestHandler<>)));
        cfg.Services.AddScoped(typeof(IRequestHandler<,>), typeof(DapperQueryRequestHandler<>));
        cfg.Services.AddScoped(typeof(IRequestHandler<,>), typeof(DapperFirstQueryRequestHandler<>));
        cfg.Services.AddScopedAsImplementedInterfaces<DapperScalarRequestHandler>();
        return cfg;
    }
}