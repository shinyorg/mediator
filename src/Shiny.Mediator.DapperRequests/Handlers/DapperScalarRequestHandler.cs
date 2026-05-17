using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;


/// <summary>
/// Request handler for <see cref="DapperScalar"/>. Executes the interpolated SQL via Dapper's
/// <c>ExecuteScalarAsync</c> and returns the first column of the first row as <see cref="object"/>.
/// </summary>
public class DapperScalarRequestHandler(IConnectionProvider connectionProvider) : IRequestHandler<DapperScalar, object>
{
    /// <inheritdoc/>
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
