namespace Shiny.Mediator;

public record DapperQuery<TResult>(FormattableString Sql, int? CommandTimeout = null) : IRequest<IEnumerable<TResult>>;
public record DapperFirstQuery<TResult>(FormattableString Sql, int? CommandTimeout = null) : IRequest<TResult>;
public record DapperScalar(FormattableString Sql, int? CommandTimeout = null) : IRequest<object>;
