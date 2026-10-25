namespace H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;

/// <summary>
///     Represents an H2 array ALL expression.
/// </summary>
/// <remarks>
///     See https://www.h2database.com/html/functions.html
/// </remarks>
public class H2AllExpression : SqlExpression, IEquatable<H2AllExpression>
{
    private static ConstructorInfo? _quotingConstructor;

    /// <inheritdoc />
    public override Type Type
        => typeof(bool);

    /// <summary>
    ///     The value to test against the <see cref="Array" />.
    /// </summary>
    public virtual SqlExpression Item { get; }

    /// <summary>
    ///     The array of values or patterns to test for the <see cref="Item" />.
    /// </summary>
    public virtual SqlExpression Array { get; }

    /// <summary>
    ///     The operator.
    /// </summary>
    public virtual H2AllOperatorType OperatorType { get; }

    /// <summary>
    ///     Constructs a <see cref="H2AllExpression" />.
    /// </summary>
    /// <param name="operatorType">The operator symbol to the array expression.</param>
    /// <param name="item">The value to find.</param>
    /// <param name="array">The array to search.</param>
    /// <param name="typeMapping">The type mapping for the expression.</param>
    public H2AllExpression(
        SqlExpression item,
        SqlExpression array,
        H2AllOperatorType operatorType,
        RelationalTypeMapping? typeMapping)
        : base(typeof(bool), typeMapping)
    {
        if (array.Type.TryGetElementType(typeof(IEnumerable<>)) is null)
        {
            throw new ArgumentException("Array expression must be an IEnumerable", nameof(array));
        }

        Item = item;
        Array = array;
        OperatorType = operatorType;
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will
    ///     return this expression.
    /// </summary>
    /// <param name="item">The <see cref="Item" /> property of the result.</param>
    /// <param name="array">The <see cref="Array" /> property of the result.</param>
    /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
    public virtual H2AllExpression Update(SqlExpression item, SqlExpression array)
        => item != Item || array != Array
            ? new H2AllExpression(item, array, OperatorType, TypeMapping)
            : this;

    /// <inheritdoc />
    public override Expression Quote()
        => New(
            _quotingConstructor ??= typeof(H2AllExpression).GetConstructor(
                [typeof(SqlExpression), typeof(SqlExpression), typeof(H2AllOperatorType), typeof(RelationalTypeMapping)])!,
            Item.Quote(),
            Array.Quote(),
            Constant(OperatorType),
            RelationalExpressionQuotingUtilities.QuoteTypeMapping(TypeMapping));

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor)
        => Update((SqlExpression)visitor.Visit(Item), (SqlExpression)visitor.Visit(Array));

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is H2AllExpression e && Equals(e);

    /// <inheritdoc />
    public virtual bool Equals(H2AllExpression? other)
        => ReferenceEquals(this, other)
            || other is not null
            && base.Equals(other)
            && Item.Equals(other.Item)
            && Array.Equals(other.Array)
            && OperatorType.Equals(other.OperatorType);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), Item, Array, OperatorType);

    /// <inheritdoc />
    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Visit(Item);
        expressionPrinter
            .Append(" ")
            .Append(
                OperatorType switch
                {
                    H2AllOperatorType.Like => "LIKE",
                    H2AllOperatorType.ILike => "ILIKE",

                    _ => throw new ArgumentOutOfRangeException($"Unhandled operator type: {OperatorType}")
                })
            .Append(" ALL(");
        expressionPrinter.Visit(Array);
        expressionPrinter.Append(")");
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{Item} {OperatorType} ALL({Array})";
}

/// <summary>
///     Determines the operator type for a <see cref="H2AllExpression" />.
/// </summary>
public enum H2AllOperatorType
{
    /// <summary>
    ///     Represents an H2 LIKE ALL operator.
    /// </summary>
    Like,

    /// <summary>
    ///     Represents an H2 ILIKE ALL operator.
    /// </summary>
    ILike,
}
