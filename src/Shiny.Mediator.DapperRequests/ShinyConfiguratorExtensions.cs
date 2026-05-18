using System.Data;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.DapperRequests;
using Shiny.Mediator.DapperRequests.Handlers;

namespace Shiny.Mediator;


/// <summary>
/// Registration extensions for the Dapper-backed request handlers
/// (<see cref="DapperQuery{TResult}"/>, <see cref="DapperFirstQuery{TResult}"/>, and
/// <see cref="DapperScalar"/>).
/// </summary>
public static class ShinyConfiguratorExtensions
{
    /// <summary>
    /// Registers the Dapper request handlers backed by a single shared connection string. A new
    /// <typeparamref name="TConnection"/> instance is constructed per request via
    /// <see cref="SingleConnectionProvider{TConnection}"/>.
    /// </summary>
    /// <typeparam name="TConnection">The ADO.NET connection type to instantiate (e.g. <c>SqlConnection</c>).</typeparam>
    /// <param name="cfg">The mediator builder to wire into.</param>
    /// <param name="connectionString">The ADO.NET connection string used for every Dapper request.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ShinyMediatorBuilder AddDapper<TConnection>(this ShinyMediatorBuilder cfg, string connectionString)
        where TConnection : class, IDbConnection, new()
    {
        cfg.Services.AddSingleton<IConnectionProvider>(new SingleConnectionProvider<TConnection>(connectionString));
        return cfg.AddInfrastructure();
    }


    /// <summary>
    /// Registers the Dapper request handlers using a custom <see cref="IConnectionProvider"/>
    /// implementation. Use this overload when connection selection depends on the incoming request
    /// (e.g. multi-tenant routing, read/write splits).
    /// </summary>
    /// <typeparam name="TConnectionProvider">The provider type that supplies <see cref="IDbConnection"/> instances per request.</typeparam>
    /// <param name="cfg">The mediator builder to wire into.</param>
    /// <returns>The same builder for chaining.</returns>
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
