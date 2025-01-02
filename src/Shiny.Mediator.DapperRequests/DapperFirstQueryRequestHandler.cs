using InterpolatedSql.Dapper;
using InterpolatedSql.SqlBuilders;

namespace Shiny.Mediator.DapperRequests;


public class DapperFirstQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperFirstQuery<TResult>, TResult>
{
    public async Task<TResult> Handle(DapperFirstQuery<TResult> request, CancellationToken cancellationToken)
    {
        var conn = connectionProvider.Create(request);
        
        var result = await conn
            .QueryBuilder(request.Sql)
            .QueryFirstOrDefaultAsync<TResult>(
                commandTimeout: request.CommandTimeout,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return result;
    }
}