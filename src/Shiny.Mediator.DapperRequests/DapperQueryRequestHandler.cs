using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests;


public class DapperQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperQuery<TResult>, IEnumerable<TResult>> 
{
    public async Task<IEnumerable<TResult>> Handle(DapperQuery<TResult> request, CancellationToken cancellationToken)
    {
        // I could have a command interceptor with middleware
        var conn = connectionProvider.Create(request);
        var result = await conn
            .QueryBuilder(request.Sql)
            .QueryAsync<TResult>(
                commandTimeout: request.CommandTimeout,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return result;
    }
}