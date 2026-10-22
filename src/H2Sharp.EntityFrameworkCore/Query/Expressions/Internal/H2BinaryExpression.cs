using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

namespace H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;

/// <summary>
///     An expression that represents a PostgreSQL-specific binary operation in a SQL tree.
/// </summary>
public class H2BinaryExpression : SqlExpression
{
    private static ConstructorInfo? _quotingConstructor;

    /// <summary>
    ///     Creates a new instance of the <see cref="H2BinaryExpression" /> class.
    /// </summary>
    /// <param name="operatorType">The operator to apply.</param>
    /// <param name="left">An expression which is left operand.</param>
    /// <param name="right">An expression which is right operand.</param>
    /// <param name="type">The <see cref="Type" /> of the expression.</param>
    /// <param name="typeMapping">The <see cref="RelationalTypeMapping" /> associated with the expression.</param>
    public H2BinaryExpression(
        H2ExpressionType operatorType,
        SqlExpression left,
        SqlExpression right,
        Type type,
        RelationalTypeMapping? typeMapping)
        : base(type, typeMapping)
    {
        Check.NotNull(left, nameof(left));
        Check.NotNull(right, nameof(right));

        OperatorType = operatorType;
        Left = left;
        Right = right;
    }

    /// <summary>
    ///     The operator of this PostgreSQL binary operation.
    /// </summary>
    public virtual H2ExpressionType OperatorType { get; }

    /// <summary>
    ///     The left operand.
    /// </summary>
    public virtual SqlExpression Left { get; }

    /// <summary>
    ///     The right operand.
    /// </summary>
    public virtual SqlExpression Right { get; }

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        Check.NotNull(visitor, nameof(visitor));

        var left = (SqlExpression)visitor.Visit(Left);
        var right = (SqlExpression)visitor.Visit(Right);

        return Update(left, right);
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will
    ///     return this expression.
    /// </summary>
    /// <param name="left">The <see cref="Left" /> property of the result.</param>
    /// <param name="right">The <see cref="Right" /> property of the result.</param>
    /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
    public virtual H2BinaryExpression Update(SqlExpression left, SqlExpression right)
    {
        Check.NotNull(left, nameof(left));
        Check.NotNull(right, nameof(right));

        return left != Left || right != Right
            ? new H2BinaryExpression(OperatorType, left, right, Type, TypeMapping)
            : this;
    }

    /// <inheritdoc />
    public override Expression Quote()
        => New(
            _quotingConstructor ??= typeof(H2BinaryExpression).GetConstructor(
                [typeof(H2ExpressionType), typeof(SqlExpression), typeof(SqlExpression), typeof(Type), typeof(RelationalTypeMapping)])!,
            Constant(OperatorType),
            Left.Quote(),
            Right.Quote(),
            Constant(Type),
            RelationalExpressionQuotingUtilities.QuoteTypeMapping(TypeMapping));

    /// <inheritdoc />
    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        Check.NotNull(expressionPrinter, nameof(expressionPrinter));

        var requiresBrackets = RequiresBrackets(Left);

        if (requiresBrackets)
        {
            expressionPrinter.Append("(");
        }

        expressionPrinter.Visit(Left);

        if (requiresBrackets)
        {
            expressionPrinter.Append(")");
        }

        expressionPrinter
            .Append(" ")
            .Append(
                OperatorType switch
                {
                    H2ExpressionType.Contains => "@>",
                    H2ExpressionType.ContainedBy => "<@",
                    H2ExpressionType.Overlaps => "&&",

                    H2ExpressionType.NetworkContainedByOrEqual => "<<=",
                    H2ExpressionType.NetworkContainsOrEqual => ">>=",
                    H2ExpressionType.NetworkContainsOrContainedBy => "&&",

                    H2ExpressionType.RangeIsStrictlyLeftOf => "<<",
                    H2ExpressionType.RangeIsStrictlyRightOf => ">>",
                    H2ExpressionType.RangeDoesNotExtendRightOf => "&<",
                    H2ExpressionType.RangeDoesNotExtendLeftOf => "&>",
                    H2ExpressionType.RangeIsAdjacentTo => "-|-",
                    H2ExpressionType.RangeUnion => "+",
                    H2ExpressionType.RangeIntersect => "*",
                    H2ExpressionType.RangeExcept => "-",

                    H2ExpressionType.TextSearchMatch => "@@",
                    H2ExpressionType.TextSearchAnd => "&&",
                    H2ExpressionType.TextSearchOr => "||",

                    H2ExpressionType.JsonExists => "?",
                    H2ExpressionType.JsonExistsAny => "?|",
                    H2ExpressionType.JsonExistsAll => "?&",

                    H2ExpressionType.LTreeMatches
                        when Right.TypeMapping is { StoreType: "lquery" } or H2ArrayTypeMapping
                        {
                            ElementTypeMapping.StoreType: "lquery"
                        }
                        => "~",
                    H2ExpressionType.LTreeMatches when Right.TypeMapping?.StoreType == "ltxtquery" => "@",
                    H2ExpressionType.LTreeMatchesAny => "?",
                    H2ExpressionType.LTreeFirstAncestor => "?@>",
                    H2ExpressionType.LTreeFirstDescendent => "?<@",
                    H2ExpressionType.LTreeFirstMatches when Right.TypeMapping?.StoreType == "lquery" => "?~",
                    H2ExpressionType.LTreeFirstMatches when Right.TypeMapping?.StoreType == "ltxtquery" => "?@",

                    H2ExpressionType.Distance => "<->",

                    _ => throw new ArgumentOutOfRangeException($"Unhandled operator type: {OperatorType}")
                })
            .Append(" ");

        requiresBrackets = RequiresBrackets(Right);

        if (requiresBrackets)
        {
            expressionPrinter.Append("(");
        }

        expressionPrinter.Visit(Right);

        if (requiresBrackets)
        {
            expressionPrinter.Append(")");
        }

        static bool RequiresBrackets(SqlExpression expression)
            => expression is H2BinaryExpression or LikeExpression;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is not null
            && (ReferenceEquals(this, obj)
                || obj is H2BinaryExpression sqlBinaryExpression
                && Equals(sqlBinaryExpression));

    private bool Equals(H2BinaryExpression sqlBinaryExpression)
        => base.Equals(sqlBinaryExpression)
            && OperatorType == sqlBinaryExpression.OperatorType
            && Left.Equals(sqlBinaryExpression.Left)
            && Right.Equals(sqlBinaryExpression.Right);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), OperatorType, Left, Right);
}
