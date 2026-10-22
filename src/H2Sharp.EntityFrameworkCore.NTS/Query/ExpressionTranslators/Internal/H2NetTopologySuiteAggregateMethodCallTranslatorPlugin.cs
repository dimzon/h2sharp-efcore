using NetTopologySuite.Geometries.Utilities;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     Translates geometry aggregates supported by H2 2.5.
/// </summary>
public class H2NetTopologySuiteAggregateMethodCallTranslatorPlugin : IAggregateMethodCallTranslatorPlugin
{
    /// <summary>
    ///     Creates the H2 geometry aggregate translators.
    /// </summary>
    public H2NetTopologySuiteAggregateMethodCallTranslatorPlugin(
        IRelationalTypeMappingSource typeMappingSource,
        ISqlExpressionFactory sqlExpressionFactory)
    {
        if (sqlExpressionFactory is not H2SqlExpressionFactory h2SqlExpressionFactory)
        {
            throw new ArgumentException($"Must be an {nameof(H2SqlExpressionFactory)}", nameof(sqlExpressionFactory));
        }

        Translators = [new H2NetTopologySuiteAggregateMethodTranslator(h2SqlExpressionFactory, typeMappingSource)];
    }

    /// <inheritdoc />
    public virtual IEnumerable<IAggregateMethodCallTranslator> Translators { get; }
}

/// <summary>
///     Translates EnvelopeCombiner to H2's minimum bounding-box aggregate.
/// </summary>
public class H2NetTopologySuiteAggregateMethodTranslator : IAggregateMethodCallTranslator
{
    private static readonly MethodInfo EnvelopeCombineMethod
        = typeof(EnvelopeCombiner).GetRuntimeMethod(nameof(EnvelopeCombiner.CombineAsGeometry), [typeof(IEnumerable<Geometry>)])!;

    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    /// <summary>
    ///     Creates an H2 geometry aggregate translator.
    /// </summary>
    public H2NetTopologySuiteAggregateMethodTranslator(
        H2SqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _typeMappingSource = typeMappingSource;
    }

    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        MethodInfo method,
        EnumerableExpression source,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method != EnvelopeCombineMethod || source.Selector is not SqlExpression expression)
        {
            return null;
        }

        return _sqlExpressionFactory.AggregateFunction(
            "ENVELOPE",
            [expression],
            source,
            nullable: true,
            argumentsPropagateNullability: [false],
            typeof(Geometry),
            _typeMappingSource.FindMapping(typeof(Geometry), "geometry"));
    }
}
