using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;


public class DapperScalarRequestHandler(IConnectionProvider connectionProvider) : IRequestHandler<DapperScalar, object>
{
    public Task<object> Handle(DapperScalar request, IMediatorContext context, CancellationToken cancellationToken)
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