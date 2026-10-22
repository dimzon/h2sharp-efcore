using H2Sharp.EntityFrameworkCore.Query.Expressions;
using H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2JsonDbFunctionsTranslator : IMethodCallTranslator
{
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly RelationalTypeMapping _stringTypeMapping;
    private readonly RelationalTypeMapping _jsonbTypeMapping;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2JsonDbFunctionsTranslator(
        IRelationalTypeMappingSource typeMappingSource,
        H2SqlExpressionFactory sqlExpressionFactory,
        IModel model)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _stringTypeMapping = typeMappingSource.FindMapping(typeof(string), model)!;
        _jsonbTypeMapping = typeMappingSource.FindMapping("jsonb")!;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(H2JsonDbFunctionsExtensions))
        {
            return null;
        }

        var args = arguments
            // Skip useless DbFunctions instance
            .Skip(1)
            // JSON extensions accept object parameters for JSON, since they must be able to handle POCOs, strings or DOM types.
            // This means they come wrapped in a convert node, which we need to remove.
            // Convert nodes may also come from wrapping JsonTraversalExpressions generated through POCO traversal.
            .Select(RemoveConvert)
            // If a function is invoked over a JSON traversal expression, that expression may come with
            // returnText: true (i.e. operator ->> and not ->). Since the functions below require a json object and
            // not text, we transform it.
            .Select(a => a is H2JsonTraversalExpression traversal ? WithReturnsText(traversal, false) : a)
            .ToArray();

        if (!args.Any(a => a.TypeMapping is H2JsonTypeMapping || a is H2JsonTraversalExpression))
        {
            throw new InvalidOperationException("The EF JSON methods require a JSON parameter and none was found.");
        }

        if (method.Name == nameof(H2JsonDbFunctionsExtensions.JsonTypeof))
        {
            return _sqlExpressionFactory.Function(
                ((H2JsonTypeMapping)args[0].TypeMapping!).IsJsonb ? "jsonb_typeof" : "json_typeof",
                [args[0]],
                nullable: true,
                argumentsPropagateNullability: TrueArrays[1],
                typeof(string));
        }

        // The following are jsonb-only, not support on json
        if (args.Any(a => a.TypeMapping is H2JsonTypeMapping { IsJsonb: false }))
        {
            throw new InvalidOperationException("JSON methods on EF.Functions only support the jsonb type, not json.");
        }

        return method.Name switch
        {
            nameof(H2JsonDbFunctionsExtensions.JsonContains)
                => _sqlExpressionFactory.Contains(Jsonb(args[0]), Jsonb(args[1])),
            nameof(H2JsonDbFunctionsExtensions.JsonContained)
                => _sqlExpressionFactory.ContainedBy(Jsonb(args[0]), Jsonb(args[1])),
            nameof(H2JsonDbFunctionsExtensions.JsonExists)
                => _sqlExpressionFactory.MakeH2Binary(H2ExpressionType.JsonExists, Jsonb(args[0]), args[1]),
            nameof(H2JsonDbFunctionsExtensions.JsonExistAny)
                => _sqlExpressionFactory.MakeH2Binary(H2ExpressionType.JsonExistsAny, Jsonb(args[0]), args[1]),
            nameof(H2JsonDbFunctionsExtensions.JsonExistAll)
                => _sqlExpressionFactory.MakeH2Binary(H2ExpressionType.JsonExistsAll, Jsonb(args[0]), args[1]),

            _ => null
        };

        SqlExpression Jsonb(SqlExpression e)
            => e.TypeMapping?.StoreType == "jsonb"
                ? e
                : e is SqlConstantExpression or SqlParameterExpression
                    ? _sqlExpressionFactory.ApplyTypeMapping(e, _jsonbTypeMapping)
                    : _sqlExpressionFactory.Convert(e, typeof(string), _jsonbTypeMapping);

        static SqlExpression RemoveConvert(SqlExpression e)
        {
            while (e is SqlUnaryExpression { OperatorType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                e = unary.Operand;
            }

            return e;
        }

        H2JsonTraversalExpression WithReturnsText(H2JsonTraversalExpression traversal, bool returnsText)
            => traversal.ReturnsText == returnsText
                ? traversal
                : returnsText
                    ? new H2JsonTraversalExpression(traversal.Expression, traversal.Path, true, typeof(string), _stringTypeMapping)
                    : new H2JsonTraversalExpression(
                        traversal.Expression, traversal.Path, false, traversal.Type, traversal.Expression.TypeMapping);
    }
}
