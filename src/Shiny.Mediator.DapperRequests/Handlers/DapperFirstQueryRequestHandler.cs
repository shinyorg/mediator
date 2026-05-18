using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;


/// <summary>
/// Request handler for <see cref="DapperFirstQuery{TResult}"/>. Executes the interpolated SQL via
/// Dapper's <c>QueryFirstOrDefaultAsync</c> and returns the first row, or
/// <c>default(TResult)</c> when none is produced.
/// </summary>
public class DapperFirstQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperFirstQuery<TResult>, TResult>
{
    /// <inheritdoc/>
    public Task<TResult> Handle(
        DapperFirstQuery<TResult> request,
        IMediatorContext context,
        CancellationToken cancellationToken
    ) => connectionProvider
        .Create(request)
        .QueryBuilder(request.Sql)
        .QueryFirstOrDefaultAsync<TResult>(
            request.Transaction,
            request.CommandTimeout,
            request.CommandType,
            cancellationToken
        );
}
