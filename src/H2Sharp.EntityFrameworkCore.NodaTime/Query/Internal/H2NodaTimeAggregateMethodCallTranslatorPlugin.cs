using H2Sharp.EntityFrameworkCore.Query;

namespace H2Sharp.EntityFrameworkCore.NodaTime.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2NodaTimeAggregateMethodCallTranslatorPlugin : IAggregateMethodCallTranslatorPlugin
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2NodaTimeAggregateMethodCallTranslatorPlugin(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        if (sqlExpressionFactory is not H2SqlExpressionFactory npgsqlSqlExpressionFactory)
        {
            throw new ArgumentException($"Must be an {nameof(H2SqlExpressionFactory)}", nameof(sqlExpressionFactory));
        }

        Translators =
        [
            new H2NodaTimeAggregateMethodTranslator(npgsqlSqlExpressionFactory, typeMappingSource)
        ];
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IEnumerable<IAggregateMethodCallTranslator> Translators { get; }
}

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2NodaTimeAggregateMethodTranslator : IAggregateMethodCallTranslator
{
    private static readonly bool[][] FalseArrays = [[], [false]];

    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2NodaTimeAggregateMethodTranslator(
        H2SqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _typeMappingSource = typeMappingSource;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? Translate(
        MethodInfo method,
        EnumerableExpression source,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (source.Selector is not SqlExpression sqlExpression || method.DeclaringType != typeof(H2NodaTimeDbFunctionsExtensions))
        {
            return null;
        }

        return method.Name switch
        {
            nameof(H2NodaTimeDbFunctionsExtensions.Sum) => _sqlExpressionFactory.AggregateFunction(
                "sum", [sqlExpression], source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: sqlExpression.Type, sqlExpression.TypeMapping),

            nameof(H2NodaTimeDbFunctionsExtensions.Average) => _sqlExpressionFactory.AggregateFunction(
                "avg", [sqlExpression], source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: sqlExpression.Type, sqlExpression.TypeMapping),

            nameof(H2NodaTimeDbFunctionsExtensions.RangeAgg) => _sqlExpressionFactory.AggregateFunction(
                "range_agg", [sqlExpression], source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: method.ReturnType, _typeMappingSource.FindMapping(method.ReturnType)),

            nameof(H2NodaTimeDbFunctionsExtensions.RangeIntersectAgg) => _sqlExpressionFactory.AggregateFunction(
                "range_intersect_agg", [sqlExpression], source, nullable: true, argumentsPropagateNullability: FalseArrays[1],
                returnType: sqlExpression.Type, sqlExpression.TypeMapping),

            _ => null
        };
    }
}
