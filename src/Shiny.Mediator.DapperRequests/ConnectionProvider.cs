using System.Data;

namespace Shiny.Mediator.DapperRequests;


public interface IConnectionProvider
{
    IDbConnection Create<TRequest>(TRequest request);
}


public class SingleConnectionProvider<TConnection>(string connectionString) : IConnectionProvider where TConnection : class, IDbConnection, new()
{
    public IDbConnection Create<TRequest>(TRequest request) => new TConnection
    {
        ConnectionString = connectionString
    };
}
