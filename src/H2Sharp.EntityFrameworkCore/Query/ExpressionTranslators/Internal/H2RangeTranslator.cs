using H2Sharp.EntityFrameworkCore.Query.Expressions;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;
using ExpressionExtensions = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2RangeTranslator : IMethodCallTranslator, IMemberTranslator
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly IModel _model;
    private readonly bool _supportsMultiranges;

    private static readonly MethodInfo EnumerableAnyWithoutPredicate =
        typeof(Enumerable).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Single(mi => mi.Name == nameof(Enumerable.Any) && mi.GetParameters().Length == 1);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2RangeTranslator(
        IRelationalTypeMappingSource typeMappingSource,
        H2SqlExpressionFactory npgsqlSqlExpressionFactory,
        IModel model,
        bool supportsMultiranges)
    {
        _typeMappingSource = typeMappingSource;
        _sqlExpressionFactory = npgsqlSqlExpressionFactory;
        _model = model;
        _supportsMultiranges = supportsMultiranges;
    }

    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        // Any() over multirange -> NOT isempty(). H2Range<T> has IsEmpty which is translated below.
        if (_supportsMultiranges
            && method.IsGenericMethod
            && method.GetGenericMethodDefinition() == EnumerableAnyWithoutPredicate
            && arguments[0].IsMultirange())
        {
            return _sqlExpressionFactory.Not(
                _sqlExpressionFactory.Function(
                    "isempty",
                    [arguments[0]],
                    nullable: true,
                    argumentsPropagateNullability: TrueArrays[1],
                    typeof(bool)));
        }

        if (method.DeclaringType != typeof(H2RangeDbFunctionsExtensions)
            && (method.DeclaringType != typeof(H2MultirangeDbFunctionsExtensions) || !_supportsMultiranges))
        {
            return null;
        }

        if (method.Name == nameof(H2RangeDbFunctionsExtensions.Merge))
        {
            if (method.DeclaringType == typeof(H2RangeDbFunctionsExtensions))
            {
                var inferredMapping = ExpressionExtensions.InferTypeMapping(arguments[0], arguments[1]);

                return _sqlExpressionFactory.Function(
                    "range_merge",
                    [
                        _sqlExpressionFactory.ApplyTypeMapping(arguments[0], inferredMapping),
                        _sqlExpressionFactory.ApplyTypeMapping(arguments[1], inferredMapping)
                    ],
                    nullable: true,
                    argumentsPropagateNullability: TrueArrays[2],
                    method.ReturnType,
                    inferredMapping);
            }

            if (method.DeclaringType == typeof(H2MultirangeDbFunctionsExtensions))
            {
                var returnTypeMapping = arguments[0].TypeMapping is H2MultirangeTypeMapping multirangeTypeMapping
                    ? multirangeTypeMapping.RangeMapping
                    : null;

                return _sqlExpressionFactory.Function(
                    "range_merge",
                    [arguments[0]],
                    nullable: true,
                    argumentsPropagateNullability: TrueArrays[1],
                    method.ReturnType,
                    returnTypeMapping);
            }
        }

        return method.Name switch
        {
            nameof(H2RangeDbFunctionsExtensions.Contains)
                => _sqlExpressionFactory.Contains(arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.ContainedBy)
                => _sqlExpressionFactory.ContainedBy(arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.Overlaps)
                => _sqlExpressionFactory.Overlaps(arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.IsStrictlyLeftOf)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeIsStrictlyLeftOf, arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.IsStrictlyRightOf)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeIsStrictlyRightOf, arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.DoesNotExtendRightOf)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeDoesNotExtendRightOf, arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.DoesNotExtendLeftOf)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeDoesNotExtendLeftOf, arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.IsAdjacentTo)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeIsAdjacentTo, arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.Union)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeUnion, arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.Intersect)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeIntersect, arguments[0], arguments[1]),
            nameof(H2RangeDbFunctionsExtensions.Except)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.RangeExcept, arguments[0], arguments[1]),

            _ => null
        };
    }

    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        var type = member.DeclaringType;
        if (type is null || !type.IsGenericType || type.GetGenericTypeDefinition() != typeof(H2Range<>))
        {
            return null;
        }

        if (member.Name is nameof(H2Range<int>.LowerBound) or nameof(H2Range<int>.UpperBound))
        {
            var typeMapping = instance!.TypeMapping is H2RangeTypeMapping rangeMapping
                ? rangeMapping.SubtypeMapping
                : _typeMappingSource.FindMapping(returnType, _model);

            return _sqlExpressionFactory.Function(
                member.Name == nameof(H2Range<int>.LowerBound) ? "lower" : "upper",
                [instance],
                nullable: true,
                argumentsPropagateNullability: TrueArrays[1],
                returnType,
                typeMapping);
        }

        return member.Name switch
        {
            nameof(H2Range<int>.IsEmpty) => SingleArgBoolFunction("isempty", instance!),
            nameof(H2Range<int>.LowerBoundIsInclusive) => SingleArgBoolFunction("lower_inc", instance!),
            nameof(H2Range<int>.UpperBoundIsInclusive) => SingleArgBoolFunction("upper_inc", instance!),
            nameof(H2Range<int>.LowerBoundInfinite) => SingleArgBoolFunction("lower_inf", instance!),
            nameof(H2Range<int>.UpperBoundInfinite) => SingleArgBoolFunction("upper_inf", instance!),

            _ => null
        };

        SqlExpression SingleArgBoolFunction(string name, SqlExpression argument)
            => _sqlExpressionFactory.Function(
                name,
                [argument],
                nullable: true,
                argumentsPropagateNullability: TrueArrays[1],
                typeof(bool));
    }
}
