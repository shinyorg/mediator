using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;


public class DapperFirstQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperFirstQuery<TResult>, TResult>
{
    public Task<TResult> Handle(DapperFirstQuery<TResult> request, CancellationToken cancellationToken)
        => connectionProvider
            .Create(request)
            .QueryBuilder(request.Sql)
            .QueryFirstOrDefaultAsync<TResult>(
                commandTimeout: request.CommandTimeout,
                cancellationToken: cancellationToken
            );
}