using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;

public class DapperQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperQuery<TResult>, IEnumerable<TResult>>
{
    public Task<IEnumerable<TResult>> Handle(
        DapperQuery<TResult> request, 
        RequestContext<DapperQuery<TResult>> context, 
        CancellationToken cancellationToken
    ) => connectionProvider
        .Create(request)
        .QueryBuilder(request.Sql)
        .QueryAsync<TResult>(
            null,
            request.CommandTimeout,
            cancellationToken: cancellationToken
        );
}