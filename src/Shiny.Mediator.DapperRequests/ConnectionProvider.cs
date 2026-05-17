using System.Data;

namespace Shiny.Mediator.DapperRequests;


/// <summary>
/// Supplies an <see cref="IDbConnection"/> for a given Dapper-backed mediator request. Implement
/// this to control connection selection per request (for example, multi-tenant routing or
/// read/write splits).
/// </summary>
public interface IConnectionProvider
{
    /// <summary>
    /// Creates a new connection for the supplied request. The Dapper handlers own the returned
    /// connection and dispose it once the query completes.
    /// </summary>
    IDbConnection Create<TRequest>(TRequest request);
}


/// <summary>
/// Default <see cref="IConnectionProvider"/> that returns a new <typeparamref name="TConnection"/>
/// instance configured with a single fixed connection string for every request.
/// </summary>
/// <typeparam name="TConnection">The ADO.NET connection type to instantiate.</typeparam>
public class SingleConnectionProvider<TConnection>(string connectionString) : IConnectionProvider where TConnection : class, IDbConnection, new()
{
    /// <inheritdoc/>
    public IDbConnection Create<TRequest>(TRequest request) => new TConnection
    {
        ConnectionString = connectionString
    };
}
