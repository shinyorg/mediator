using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests;

public class DapperScalarQueryRequestHandler(IConnectionProvider connectionProvider) : IRequestHandler<DapperScalarQuery, object>
{
    public async Task<object> Handle(DapperScalarQuery request, CancellationToken cancellationToken)
    {
        var conn = connectionProvider.Create(request);
        var result = await conn
            .QueryBuilder(request.Sql)
            .ExecuteScalarAsync(
                commandTimeout: request.CommandTimeout,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return result;
    }
}