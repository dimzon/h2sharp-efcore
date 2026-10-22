using System.Text.Json.Serialization;
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
public class H2JsonPocoTranslator : IMemberTranslator, IMethodCallTranslator
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly RelationalTypeMapping _stringTypeMapping;
    private readonly IModel _model;

    private static readonly MethodInfo Enumerable_AnyWithoutPredicate =
        typeof(Enumerable).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Single(mi => mi.Name == nameof(Enumerable.Any) && mi.GetParameters().Length == 1);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2JsonPocoTranslator(
        IRelationalTypeMappingSource typeMappingSource,
        H2SqlExpressionFactory sqlExpressionFactory,
        IModel model)
    {
        _typeMappingSource = typeMappingSource;
        _sqlExpressionFactory = sqlExpressionFactory;
        _model = model;
        _stringTypeMapping = typeMappingSource.FindMapping(typeof(string), model)!;
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
        // Predicate-less Any - translate to a simple length check.
        if (method.IsClosedFormOf(Enumerable_AnyWithoutPredicate)
            && TranslateArrayLength(arguments[0]) is SqlExpression arrayLengthTranslation)
        {
            return _sqlExpressionFactory.GreaterThan(arrayLengthTranslation, _sqlExpressionFactory.Constant(0));
        }

        return null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance?.TypeMapping is not H2JsonTypeMapping && instance is not H2JsonTraversalExpression)
        {
            return null;
        }

        if (member is { Name: "Count", DeclaringType.IsGenericType: true }
            && member.DeclaringType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return TranslateArrayLength(instance);
        }

        return TranslateMemberAccess(
            instance,
            _sqlExpressionFactory.Constant(member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name),
            returnType);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? TranslateMemberAccess(SqlExpression instance, SqlExpression member, Type returnType)
    {
        return instance switch
        {
            // The first time we see a JSON traversal it's on a column - create a JsonTraversalExpression.
            // Traversals on top of that get appended into the same expression.
            ColumnExpression { TypeMapping: H2JsonTypeMapping } columnExpression
                => ConvertFromText(
                    _sqlExpressionFactory.JsonTraversal(
                        columnExpression,
                        [member],
                        returnsText: true,
                        typeof(string),
                        _stringTypeMapping),
                    returnType),

            H2JsonTraversalExpression prevPathTraversal
                => ConvertFromText(
                    prevPathTraversal.Append(_sqlExpressionFactory.ApplyDefaultTypeMapping(member)),
                    returnType),

            _ => null
        };

        // The PostgreSQL traversal operator always returns text.
        // If the type returned is a scalar (int, bool, etc.), we need to apply a conversion from string.
        SqlExpression ConvertFromText(SqlExpression expression, Type returnType)
            => _typeMappingSource.FindMapping(returnType.UnwrapNullableType(), _model) switch
            {
                // Type mapping not found - this isn't a scalar
                null => expression,

                // Arrays are dealt with as JSON arrays, not array scalars
                H2ArrayTypeMapping => expression,

                // Text types don't a a conversion to string
                { StoreTypeNameBase: "text" or "varchar" or "char" } => expression,

                // For any other type mapping, this is a scalar; apply a conversion to the type from string.
                var mapping => _sqlExpressionFactory.Convert(expression, returnType, mapping)
            };
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? TranslateArrayLength(SqlExpression expression)
    {
        switch (expression)
        {
            // H2 2.5 has no function that counts the elements of a JSON array: jsonb_array_length and
            // json_array_length are PostgreSQL's, and the reference knows neither. The length is
            // therefore refused during translation.
            case ColumnExpression { TypeMapping: H2JsonTypeMapping }:
            case H2JsonTraversalExpression:
                throw new NotSupportedException(
                    "H2 2.5 cannot count the elements of a JSON array: the functions jsonb_array_length and "
                    + "json_array_length are PostgreSQL's, and the reference knows neither.");

            default:
                return null;
        }
    }
}
