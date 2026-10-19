using System.Runtime.CompilerServices;

namespace H2Sharp.EntityFrameworkCore.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2EvaluatableExpressionFilter : RelationalEvaluatableExpressionFilter
{

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2EvaluatableExpressionFilter(
        EvaluatableExpressionFilterDependencies dependencies,
        RelationalEvaluatableExpressionFilterDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool IsEvaluatableExpression(Expression expression, IModel model)
    {
        switch (expression)
        {
            case MethodCallExpression methodCallExpression:
                var declaringType = methodCallExpression.Method.DeclaringType;
                var method = methodCallExpression.Method;

                if (method is null                    || method == TsVectorParse
                    || declaringType == typeof(H2DbFunctionsExtensions)
                    || declaringType == typeof(H2JsonDbFunctionsExtensions)
                    // Prevent evaluation of ValueTuple.Create, see NewExpression of ITuple below
                    || declaringType == typeof(ValueTuple) && method.Name == nameof(ValueTuple.Create))
                {
                    return false;
                }

                break;

            case NewExpression newExpression when newExpression.Type.IsAssignableTo(typeof(ITuple)):
                // We translate new ValueTuple<T1, T2...>(x, y...) to a SQL row value expression: (x, y)
                // (see H2SqlTranslatingExpressionVisitor.VisitNew).
                // We must prevent evaluation when the tuple contains only constants/parameters, since SQL row values cannot be
                // parameterized; we need to render them as "literals" instead:
                // WHERE (x, y) > (3, $1)
                return false;
        }

        return base.IsEvaluatableExpression(expression, model);
    }
}
