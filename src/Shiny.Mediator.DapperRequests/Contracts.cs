namespace Shiny.Mediator;


public record DapperRequest<TResult>(
    FormattableString Sql, 
    int? CommandTimeout = null,
    string? RequestKey = null
) : IRequest<TResult>, IRequestKey 
{
    public string GetKey()
    {
        if (this.RequestKey != null)
            return this.RequestKey;

        var key = typeof(TResult).FullName + "_" + Sql;
        return key;
    }
}
public record DapperQuery<TResult>(
    FormattableString Sql, 
    int? CommandTimeout = null,
    string? RequestKey = null
) : DapperRequest<IEnumerable<TResult>>(
    Sql,
    CommandTimeout,
    RequestKey
);

public record DapperFirstQuery<TResult>(
    FormattableString Sql, 
    int? CommandTimeout = null,
    string? RequestKey = null
) : DapperRequest<TResult>(
    Sql,
    CommandTimeout,
    RequestKey
);

public record DapperScalar(
    FormattableString Sql, 
    int? CommandTimeout = null,
    string? RequestKey = null
) : DapperRequest<object>(
    Sql,
    CommandTimeout,
    RequestKey
);
