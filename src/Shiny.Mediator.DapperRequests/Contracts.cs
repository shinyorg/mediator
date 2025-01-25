using System.Data;

namespace Shiny.Mediator;


public record DapperRequest<TResult>(
    FormattableString Sql, 
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
    IDbTransaction? Transaction = null,
    CommandType? CommandType = null,
    string? RequestKey = null
) : DapperRequest<IEnumerable<TResult>>(
    Sql,
    RequestKey
);

public record DapperFirstQuery<TResult>(
    FormattableString Sql, 
    int? CommandTimeout = null,
    IDbTransaction? Transaction = null,
    CommandType? CommandType = null,
    string? RequestKey = null
) : DapperRequest<TResult>(
    Sql,
    RequestKey
);

public record DapperScalar(
    FormattableString Sql, 
    int? CommandTimeout = null,
    IDbTransaction? Transaction = null,
    CommandType? CommandType = null,
    string? RequestKey = null
) : DapperRequest<object>(
    Sql,
    RequestKey
);
