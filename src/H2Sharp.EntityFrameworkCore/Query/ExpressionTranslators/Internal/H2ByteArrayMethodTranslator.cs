using H2Sharp.EntityFrameworkCore.Internal;
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
public class H2ByteArrayMethodTranslator : IMethodCallTranslator
{
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2ByteArrayMethodTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = (H2SqlExpressionFactory)sqlExpressionFactory;
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
        Check.NotNull(method, nameof(method));
        Check.NotNull(arguments, nameof(arguments));

        if (method.IsGenericMethod && arguments[0].TypeMapping is H2ByteArrayTypeMapping typeMapping)
        {
            // Note: we only translate if the array argument is a column mapped to a binary. There are various other
            // cases (e.g. Where(b => new byte[] { 1, 2, 3 }.Contains(b.SomeByte))) where we prefer to translate via
            // the regular array logic.
            if (method.GetGenericMethodDefinition().Equals(EnumerableMethods.Contains))
            {
                var source = arguments[0];

                // Constant-byte containment uses a binary literal. Parameterized binary searching
                // needs a separate translation; never emit PostgreSQL's set_byte for it.
                if (arguments[1] is not SqlConstantExpression constantValue)
                {
                    return null;
                }

                var value = _sqlExpressionFactory.Constant(new[] { (byte)constantValue.Value! }, typeMapping);

                return _sqlExpressionFactory.GreaterThan(
                    H2FunctionExpression.CreateWithArgumentSeparators(
                        "position",
                        [value, source],
                        ["IN"], // POSITION(x IN y)
                        nullable: true,
                        argumentsPropagateNullability: TrueArrays[2],
                        builtIn: true,
                        typeof(int),
                        null),
                    _sqlExpressionFactory.Constant(0));
            }

            if (method.GetGenericMethodDefinition().Equals(EnumerableMethods.FirstWithoutPredicate))
            {
                return _sqlExpressionFactory.ByteAt(arguments[0], _sqlExpressionFactory.Constant(0));
            }
        }

        return null;
    }
}
