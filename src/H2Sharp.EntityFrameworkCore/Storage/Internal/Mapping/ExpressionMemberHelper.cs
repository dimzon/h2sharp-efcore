using System;
using System.Linq.Expressions;
using System.Reflection;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     Gets <see cref="MemberInfo" /> for expression trees from typed lambdas, so a renamed or
///     re-signatured method is a compile error instead of a null returned by reflection.
/// </summary>
internal static class ExpressionMemberHelper
{
    public static MethodInfo Method<TDelegate>(Expression<TDelegate> expression)
        => ((MethodCallExpression)Strip(expression.Body)).Method;

    public static PropertyInfo Property<T>(Expression<Func<T>> expression)
        => (PropertyInfo)((MemberExpression)Strip(expression.Body)).Member;

    public static ConstructorInfo Constructor<T>(Expression<Func<T>> expression)
        => ((NewExpression)Strip(expression.Body)).Constructor
            ?? throw new ArgumentException("Expression is not a constructor call", nameof(expression));

    private static Expression Strip(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Convert } unary
            ? unary.Operand
            : expression;
}
