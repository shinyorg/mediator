using System.Data;

namespace Shiny.Mediator;


public record DapperRequest<TResult>(
    FormattableString Sql, 
    string? RequestKey = null
) : IRequest<TResult>, IContractKey 
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
    string? ContractKey = null
) : DapperRequest<IEnumerable<TResult>>(
    Sql,
    ContractKey
);

public record DapperFirstQuery<TResult>(
    FormattableString Sql, 
    int? CommandTimeout = null,
    IDbTransaction? Transaction = null,
    CommandType? CommandType = null,
    string? ContractKey = null
) : DapperRequest<TResult>(
    Sql,
    ContractKey
);

public record DapperScalar(
    FormattableString Sql, 
    int? CommandTimeout = null,
    IDbTransaction? Transaction = null,
    CommandType? CommandType = null,
    string? ContractKey = null
) : DapperRequest<object>(
    Sql,
    ContractKey
);
