namespace Shiny.Mediator;

// TODO: QueryBuilder
public record DapperQuery<TResult>(
    FormattableString Sql,
    int? CommandTimeout = null
) : IRequest<IEnumerable<TResult>>;

public record DapperFirstQuery<TResult>(
    FormattableString Sql,
    int? CommandTimeout = null
) : IRequest<TResult>;

public record DapperScalarQuery(
    FormattableString Sql,
    int? CommandTimeout = null
) : IRequest<object>, IRequest;