using InterpolatedSql.Dapper;

namespace Shiny.Mediator.DapperRequests.Handlers;


public class DapperFirstQueryRequestHandler<TResult>(
    IConnectionProvider connectionProvider
) : IRequestHandler<DapperFirstQuery<TResult>, TResult>
{
    public Task<TResult> Handle(
        DapperFirstQuery<TResult> request, 
        MediatorContext context, 
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