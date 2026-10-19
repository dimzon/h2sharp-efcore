using System.Collections;
using System.Data.Common;
using System.Text;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     The type mapping for PostgreSQL multirange types.
/// </summary>
/// <remarks>
///     See: https://www.postgresql.org/docs/current/static/rangetypes.html
/// </remarks>
public class H2MultirangeTypeMapping : RelationalTypeMapping
{
    /// <summary>
    ///     The relational type mapping of the ranges contained in this multirange.
    /// </summary>
    public virtual H2RangeTypeMapping RangeMapping
        => (H2RangeTypeMapping)ElementTypeMapping!;

    /// <summary>
    ///     The relational type mapping of the values contained in this multirange.
    /// </summary>
    public virtual RelationalTypeMapping SubtypeMapping { get; }

    /// <summary>
    ///     The database type used by H2.
    /// </summary>
    public virtual H2DbType H2DbType { get; }

    /// <summary>
    ///     Constructs an instance of the <see cref="H2RangeTypeMapping" /> class.
    /// </summary>
    /// <param name="storeType">The database type to map</param>
    /// <param name="clrType">The CLR type to map.</param>
    /// <param name="rangeMapping">The type mapping of the ranges contained in this multirange.</param>
    public H2MultirangeTypeMapping(string storeType, Type clrType, H2RangeTypeMapping rangeMapping)
        // TODO: Need to do comparer, converter
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(clrType, elementMapping: rangeMapping),
                storeType))
    {
        SubtypeMapping = rangeMapping.SubtypeMapping;
        H2DbType = GenerateH2DbType(rangeMapping.SubtypeMapping);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2MultirangeTypeMapping(
        RelationalTypeMappingParameters parameters,
        H2DbType npgsqlDbType)
        : base(parameters)
    {
        var rangeMapping = (H2RangeTypeMapping)parameters.CoreParameters.ElementTypeMapping!;

        SubtypeMapping = rangeMapping.SubtypeMapping;
        H2DbType = npgsqlDbType;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2MultirangeTypeMapping(parameters, H2DbType);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
        => GenerateNonNullSqlLiteral(value, RangeMapping, StoreType);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static string GenerateNonNullSqlLiteral(object value, RelationalTypeMapping rangeMapping, string multirangeStoreType)
    {
        var multirange = (IList)value;

        var sb = new StringBuilder();
        sb.Append("'{");

        for (var i = 0; i < multirange.Count; i++)
        {
            sb.Append(rangeMapping.GenerateEmbeddedSqlLiteral(multirange[i]));
            if (i < multirange.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append("}'::");
        sb.Append(multirangeStoreType);
        return sb.ToString();
    }

    private static H2DbType GenerateH2DbType(RelationalTypeMapping subtypeMapping)
    {
        H2DbType subtypeH2DbType;
        if (subtypeMapping is IH2TypeMapping npgsqlTypeMapping)
        {
            subtypeH2DbType = npgsqlTypeMapping.H2DbType;
        }
        else
        {
            // We're using a built-in, non-H2 mapping such as IntTypeMapping.
            // Infer the H2DbType from the DbType (somewhat hacky but why not).
            Debug.Assert(subtypeMapping.DbType.HasValue);
            var p = new H2Parameter { DbType = subtypeMapping.DbType.Value };
            subtypeH2DbType = p.H2DbType;
        }

        return H2DbType.Multirange | subtypeH2DbType;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Expression GenerateCodeLiteral(object value)
    {
        // Note that arrays are handled in EF Core's CSharpHelper, so this method doesn't get called for them.

        // Unfortunately, List<H2Range<T>> requires MemberInit, which CSharpHelper doesn't support
        var type = value.GetType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            throw new NotSupportedException("Cannot generate code literals for List<T>, consider using arrays instead");
        }

        throw new InvalidCastException();
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
    {
        if (parameter is not H2Parameter npgsqlParameter)
        {
            throw new ArgumentException(
                $"H2-specific type mapping {GetType()} being used with non-H2 parameter type {parameter.GetType().Name}");
        }

        npgsqlParameter.H2DbType = H2DbType;
    }
}
