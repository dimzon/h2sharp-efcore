namespace H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;

/// <summary>
///     An expression that represents an H2 <c>UNNEST</c> function call in a SQL tree.
/// </summary>
/// <remarks>
///     <para>
///         This expression is just a <see cref="H2TableValuedFunctionExpression" />, adding the ability to provide an explicit column name
///         for its output (<c>SELECT * FROM UNNEST(array) f(foo)</c>). H2 names the element column
///         <c>C1</c> and the optional ordinality column <c>NORD</c>; explicit names keep the query tree stable.
///     </para>
///     <para>
///         See <see href="https://h2database.com/html/functions.html#unnest">UNNEST</see> for more
///         information and examples.
///     </para>
///     <para>
///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///         the same compatibility standards as public APIs. It may be changed or removed without notice in
///         any release. You should only use it directly in your code with extreme caution and knowing that
///         doing so can result in application failures when updating to a new Entity Framework Core release.
///     </para>
/// </remarks>
public class H2UnnestExpression : H2TableValuedFunctionExpression
{
    /// <summary>
    ///     The array to be un-nested into a table.
    /// </summary>
    /// <remarks>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </remarks>
    public virtual SqlExpression Array
        => Arguments[0];

    /// <summary>
    ///     The name of the column to be projected out from the <c>unnest</c> call.
    /// </summary>
    /// <remarks>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </remarks>
    public virtual string ColumnName
        => ColumnInfos![0].Name;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2UnnestExpression(string alias, SqlExpression array, string columnName, bool withOrdinality = true)
        : this(alias, array, new ColumnInfo(columnName), withOrdinality)
    {
    }

    private H2UnnestExpression(string alias, SqlExpression array, ColumnInfo? columnInfo, bool withOrdinality = true)
        : base(alias, "unnest", [array], columnInfo is null ? null : [columnInfo.Value], withOrdinality)
    {
    }

    /// <inheritdoc />
    protected override Expression VisitChildren(ExpressionVisitor visitor)
        => visitor.Visit(Array) is var visitedArray && visitedArray == Array
            ? this
            : new H2UnnestExpression(Alias, (SqlExpression)visitedArray, ColumnName, WithOrdinality);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override H2UnnestExpression Update(IReadOnlyList<SqlExpression> arguments)
        => arguments is [var singleArgument]
            ? Update(singleArgument)
            : throw new ArgumentException();

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will
    ///     return this expression.
    /// </summary>
    /// <param name="array">The <see cref="Array" /> property of the result.</param>
    /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
    public virtual H2UnnestExpression Update(SqlExpression array)
        => array == Array
            ? this
            : new H2UnnestExpression(Alias, array, ColumnName, WithOrdinality);

    /// <inheritdoc />
    public override TableExpressionBase Clone(string? alias, ExpressionVisitor cloningExpressionVisitor)
        => new H2UnnestExpression(alias!, (SqlExpression)cloningExpressionVisitor.Visit(Array), ColumnName, WithOrdinality);

    /// <inheritdoc />
    public override H2UnnestExpression WithAlias(string newAlias)
        => new(newAlias, Array, ColumnName, WithOrdinality);

    /// <inheritdoc />
    public override H2UnnestExpression WithColumnInfos(IReadOnlyList<ColumnInfo> columnInfos)
        => new(
            Alias,
            Array,
            columnInfos switch
            {
                [] => null,
                [var columnInfo] => columnInfo,
                _ => throw new ArgumentException()
            },
            WithOrdinality);
}
