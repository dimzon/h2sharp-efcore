using H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2StatisticsAggregateMethodTranslator : IAggregateMethodCallTranslator
{
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly RelationalTypeMapping _doubleTypeMapping, _longTypeMapping;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2StatisticsAggregateMethodTranslator(
        H2SqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _doubleTypeMapping = typeMappingSource.FindMapping(typeof(double))!;
        _longTypeMapping = typeMappingSource.FindMapping(typeof(long))!;
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
        // Docs: https://www.postgresql.org/docs/current/functions-aggregate.html#FUNCTIONS-AGGREGATE-STATISTICS-TABLE

        if (method.DeclaringType != typeof(H2AggregateDbFunctionsExtensions)
            || source.Selector is not SqlExpression sqlExpression)
        {
            return null;
        }

        // These four functions are simple and take a single enumerable argument
        var functionName = method.Name switch
        {
            nameof(H2AggregateDbFunctionsExtensions.StandardDeviationSample) => "stddev_samp",
            nameof(H2AggregateDbFunctionsExtensions.StandardDeviationPopulation) => "stddev_pop",
            nameof(H2AggregateDbFunctionsExtensions.VarianceSample) => "var_samp",
            nameof(H2AggregateDbFunctionsExtensions.VariancePopulation) => "var_pop",
            _ => null
        };

        if (functionName is not null)
        {
            return _sqlExpressionFactory.AggregateFunction(
                functionName,
                [sqlExpression],
                source,
                nullable: true,
                argumentsPropagateNullability: FalseArrays[1],
                typeof(double),
                _doubleTypeMapping);
        }

        functionName = method.Name switch
        {
            nameof(H2AggregateDbFunctionsExtensions.Correlation) => "corr",
            nameof(H2AggregateDbFunctionsExtensions.CovariancePopulation) => "covar_pop",
            nameof(H2AggregateDbFunctionsExtensions.CovarianceSample) => "covar_samp",
            nameof(H2AggregateDbFunctionsExtensions.RegrAverageX) => "regr_avgx",
            nameof(H2AggregateDbFunctionsExtensions.RegrAverageY) => "regr_avgy",
            nameof(H2AggregateDbFunctionsExtensions.RegrCount) => "regr_count",
            nameof(H2AggregateDbFunctionsExtensions.RegrIntercept) => "regr_intercept",
            nameof(H2AggregateDbFunctionsExtensions.RegrR2) => "regr_r2",
            nameof(H2AggregateDbFunctionsExtensions.RegrSlope) => "regr_slope",
            nameof(H2AggregateDbFunctionsExtensions.RegrSXX) => "regr_sxx",
            nameof(H2AggregateDbFunctionsExtensions.RegrSXY) => "regr_sxy",
            _ => null
        };

        if (functionName is not null)
        {
            // These methods accept two enumerable (column) arguments; this is represented in LINQ as a projection from the grouping
            // to a tuple of the two columns. Since we generally translate tuples to H2RowValueExpression, we take it apart here.
            if (source.Selector is not H2RowValueExpression rowValueExpression)
            {
                return null;
            }

            var (y, x) = (rowValueExpression.Values[0], rowValueExpression.Values[1]);

            return method.Name == nameof(H2AggregateDbFunctionsExtensions.RegrCount)
                ? _sqlExpressionFactory.AggregateFunction(
                    functionName,
                    [y, x],
                    source,
                    nullable: true,
                    argumentsPropagateNullability: FalseArrays[2],
                    typeof(long),
                    _longTypeMapping)
                : _sqlExpressionFactory.AggregateFunction(
                    functionName,
                    [y, x],
                    source,
                    nullable: true,
                    argumentsPropagateNullability: FalseArrays[2],
                    typeof(double),
                    _doubleTypeMapping);
        }

        return null;
    }
}
