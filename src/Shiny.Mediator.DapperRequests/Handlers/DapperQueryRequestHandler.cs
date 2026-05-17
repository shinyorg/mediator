using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;

/// <summary>
/// Request handler for <see cref="DapperQuery{TResult}"/>. Resolves a connection from the
/// registered <see cref="IConnectionProvider"/> and executes the interpolated SQL via Dapper,
/// returning every row projected to <typeparamref name="TResult"/>.
/// </summary>
public class DapperQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperQuery<TResult>, IEnumerable<TResult>>
{
    /// <inheritdoc/>
    public Task<IEnumerable<TResult>> Handle(
        DapperQuery<TResult> request,
        IMediatorContext context,
        CancellationToken cancellationToken
    ) => connectionProvider
        .Create(request)
        .QueryBuilder(request.Sql)
        .QueryAsync<TResult>(
            request.Transaction,
            request.CommandTimeout,
            request.CommandType,
            cancellationToken
        );
}
