namespace H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;

/// <summary>
///     An SQL expression that represents a PostgreSQL DELETE operation.
/// </summary>
public sealed class H2DeleteExpression : Expression, IPrintableExpression
{
    /// <summary>
    ///     The tables that rows are to be deleted from.
    /// </summary>
    public TableExpression Table { get; }

    /// <summary>
    ///     Additional tables which can be referenced in the predicate.
    /// </summary>
    public IReadOnlyList<TableExpressionBase> FromItems { get; }

    /// <summary>
    ///     The WHERE predicate for the DELETE.
    /// </summary>
    public SqlExpression? Predicate { get; }

    /// <summary>
    ///     The list of tags applied to this <see cref="DeleteExpression" />.
    /// </summary>
    public ISet<string> Tags { get; }

    /// <summary>
    ///     Creates a new instance of the <see cref="H2DeleteExpression" /> class.
    /// </summary>
    public H2DeleteExpression(
        TableExpression table,
        IReadOnlyList<TableExpressionBase> fromItems,
        SqlExpression? predicate,
        ISet<string> tags)
    {
        (Table, FromItems, Predicate, Tags) = (table, fromItems, predicate, tags);
    }

    /// <inheritdoc />
    public override Type Type
        => typeof(object);

    /// <inheritdoc />
    public override ExpressionType NodeType
        => ExpressionType.Extension;

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor)
        => Predicate is null
            ? this
            : Update((SqlExpression?)visitor.Visit(Predicate));

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will
    ///     return this expression.
    /// </summary>
    /// <param name="predicate">The <see cref="Predicate" /> property of the result.</param>
    public H2DeleteExpression Update(SqlExpression? predicate)
        => predicate == Predicate
            ? this
            : new H2DeleteExpression(Table, FromItems, predicate, Tags);

    /// <inheritdoc />
    public void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.AppendLine($"DELETE FROM {Table.Name} AS {Table.Alias}");

        if (FromItems.Count > 0)
        {
            var first = true;
            foreach (var fromItem in FromItems)
            {
                if (first)
                {
                    expressionPrinter.Append("USING ");
                    first = false;
                }
                else
                {
                    expressionPrinter.Append(", ");
                }

                expressionPrinter.Visit(fromItem);
            }
        }

        if (Predicate is not null)
        {
            expressionPrinter.Append("WHERE ");
            expressionPrinter.Visit(Predicate);
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj != null
            && (ReferenceEquals(this, obj)
                || obj is H2DeleteExpression h2DeleteExpression
                && Equals(h2DeleteExpression));

    private bool Equals(H2DeleteExpression h2DeleteExpression)
        => Table == h2DeleteExpression.Table
            && FromItems.SequenceEqual(h2DeleteExpression.FromItems)
            && (Predicate is null ? h2DeleteExpression.Predicate is null : Predicate.Equals(h2DeleteExpression.Predicate));

    /// <inheritdoc />
    public override int GetHashCode()
        => Table.GetHashCode();
}
