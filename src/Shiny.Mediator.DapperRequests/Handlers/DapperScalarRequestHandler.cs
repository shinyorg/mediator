using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;


public class DapperScalarRequestHandler(IConnectionProvider connectionProvider) : IRequestHandler<DapperScalar, object>
{
    public Task<object> Handle(DapperScalar request, RequestContext<DapperScalar> context, CancellationToken cancellationToken)
        => connectionProvider
            .Create(request)
            .QueryBuilder(request.Sql)
            .ExecuteScalarAsync(
                request.Transaction, 
                request.CommandTimeout, 
                request.CommandType,
                cancellationToken
            );
}