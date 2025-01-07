using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;


public class DapperScalarRequestHandler(IConnectionProvider connectionProvider) : IRequestHandler<DapperScalar, object>
{
    public Task<object> Handle(DapperScalar request, CancellationToken cancellationToken)
        => connectionProvider
            .Create(request)
            .QueryBuilder(request.Sql)
            .ExecuteScalarAsync(null, request.CommandTimeout, cancellationToken: cancellationToken);
}