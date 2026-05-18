using System.Data;

namespace Shiny.Mediator;


/// <summary>
/// Base contract for the Dapper-backed mediator requests. Carries an interpolated SQL statement
/// and an optional explicit contract key; when the key is omitted, one is derived from the result
/// type and the SQL.
/// </summary>
/// <param name="Sql">The interpolated SQL statement to execute. Embedded values are bound as parameters.</param>
/// <param name="RequestKey">Optional explicit contract key used for caching and deduplication.</param>
public record DapperRequest<TResult>(
    FormattableString Sql,
    string? RequestKey = null
) : IRequest<TResult>, IContractKey
{
    /// <inheritdoc/>
    public string GetKey()
    {
        if (this.RequestKey != null)
            return this.RequestKey;

        var key = typeof(TResult).FullName + "_" + Sql;
        return key;
    }
}

/// <summary>
/// Dapper request that returns every row produced by <paramref name="Sql"/> projected to
/// <typeparamref name="TResult"/>. Dispatched via <c>DapperQueryRequestHandler</c>.
/// </summary>
/// <param name="Sql">The interpolated SQL query to execute.</param>
/// <param name="CommandTimeout">Optional command timeout in seconds.</param>
/// <param name="Transaction">Optional ambient transaction to enlist in.</param>
/// <param name="CommandType">Optional <see cref="System.Data.CommandType"/> override.</param>
/// <param name="ContractKey">Optional explicit contract key for caching and deduplication.</param>
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

/// <summary>
/// Dapper request that returns the first row of <paramref name="Sql"/> projected to
/// <typeparamref name="TResult"/>, or the type's default when no row is produced. Dispatched via
/// <c>DapperFirstQueryRequestHandler</c>.
/// </summary>
/// <param name="Sql">The interpolated SQL query to execute.</param>
/// <param name="CommandTimeout">Optional command timeout in seconds.</param>
/// <param name="Transaction">Optional ambient transaction to enlist in.</param>
/// <param name="CommandType">Optional <see cref="System.Data.CommandType"/> override.</param>
/// <param name="ContractKey">Optional explicit contract key for caching and deduplication.</param>
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

/// <summary>
/// Dapper request that executes <paramref name="Sql"/> and returns the first column of the first
/// row as an <see cref="object"/>. Dispatched via <c>DapperScalarRequestHandler</c>.
/// </summary>
/// <param name="Sql">The interpolated SQL statement to execute.</param>
/// <param name="CommandTimeout">Optional command timeout in seconds.</param>
/// <param name="Transaction">Optional ambient transaction to enlist in.</param>
/// <param name="CommandType">Optional <see cref="System.Data.CommandType"/> override.</param>
/// <param name="ContractKey">Optional explicit contract key for caching and deduplication.</param>
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
