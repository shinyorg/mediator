using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;

public class DapperQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperQuery<TResult>, IEnumerable<TResult>>
{
    public Task<IEnumerable<TResult>> Handle(
        DapperQuery<TResult> request, 
        MediatorContext context, 
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