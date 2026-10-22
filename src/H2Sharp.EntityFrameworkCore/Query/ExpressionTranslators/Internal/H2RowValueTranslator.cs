using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2RowValueTranslator : IMethodCallTranslator
{
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;

    private static readonly MethodInfo GreaterThan =
        typeof(H2DbFunctionsExtensions).GetRuntimeMethod(
            nameof(H2DbFunctionsExtensions.GreaterThan),
            [typeof(DbFunctions), typeof(ITuple), typeof(ITuple)])!;

    private static readonly MethodInfo LessThan =
        typeof(H2DbFunctionsExtensions).GetMethods()
            .Single(m => m.Name == nameof(H2DbFunctionsExtensions.LessThan));

    private static readonly MethodInfo GreaterThanOrEqual =
        typeof(H2DbFunctionsExtensions).GetMethods()
            .Single(m => m.Name == nameof(H2DbFunctionsExtensions.GreaterThanOrEqual));

    private static readonly MethodInfo LessThanOrEqual =
        typeof(H2DbFunctionsExtensions).GetMethods()
            .Single(m => m.Name == nameof(H2DbFunctionsExtensions.LessThanOrEqual));

    private static readonly Dictionary<MethodInfo, ExpressionType> ComparisonMethods = new()
    {
        { GreaterThan, ExpressionType.GreaterThan },
        { LessThan, ExpressionType.LessThan },
        { GreaterThanOrEqual, ExpressionType.GreaterThanOrEqual },
        { LessThanOrEqual, ExpressionType.LessThanOrEqual }
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="H2RowValueTranslator" /> class.
    /// </summary>
    public H2RowValueTranslator(H2SqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ValueType))] // For ValueTuple.Create
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        // Translate ValueTuple.Create
        if (method.DeclaringType == typeof(ValueTuple) && method is { IsStatic: true, Name: nameof(ValueTuple.Create) })
        {
            return new H2RowValueExpression(arguments, method.ReturnType);
        }

        // Translate EF.Functions.GreaterThan and other comparisons
        if (method.DeclaringType != typeof(H2DbFunctionsExtensions) || !ComparisonMethods.TryGetValue(method, out var expressionType))
        {
            return null;
        }

        var leftCount = arguments[1] is H2RowValueExpression leftRowValue
            ? leftRowValue.Values.Count
            : arguments[1] is SqlConstantExpression { Value : ITuple leftTuple }
                ? (int?)leftTuple.Length
                : null;

        var rightCount = arguments[2] is H2RowValueExpression rightRowValue
            ? rightRowValue.Values.Count
            : arguments[2] is SqlConstantExpression { Value : ITuple rightTuple }
                ? (int?)rightTuple.Length
                : null;

        if (leftCount is null || rightCount is null)
        {
            return null;
        }

        if (leftCount != rightCount)
        {
            throw new ArgumentException(H2Strings.RowValueComparisonRequiresTuplesOfSameLength);
        }

        return _sqlExpressionFactory.MakeBinary(expressionType, arguments[1], arguments[2], typeMapping: null);
    }
}
