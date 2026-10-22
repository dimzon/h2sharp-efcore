using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using H2Sharp.EntityFrameworkCore.Query.Expressions;
using H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

namespace H2Sharp.EntityFrameworkCore.Query.Internal;

/// <summary>
///     The default query SQL generator for H2.
/// </summary>
public class H2QuerySqlGenerator : QuerySqlGenerator
{
    private readonly ISqlGenerationHelper _sqlGenerationHelper;
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private RelationalTypeMapping? _textTypeMapping;

    /// <summary>
    ///     True if null ordering is reversed; otherwise false.
    /// </summary>
    private readonly bool _reverseNullOrderingEnabled;

    /// <inheritdoc />
    public H2QuerySqlGenerator(
        QuerySqlGeneratorDependencies dependencies,
        IRelationalTypeMappingSource typeMappingSource,
        bool reverseNullOrderingEnabled)
        : base(dependencies)
    {
        _sqlGenerationHelper = dependencies.SqlGenerationHelper;
        _typeMappingSource = typeMappingSource;
        _reverseNullOrderingEnabled = reverseNullOrderingEnabled;
    }

    /// <inheritdoc />
    protected override Expression VisitTable(TableExpression tableExpression)
    {
        // The table's alias is written without the AS: SQL allows an alias without it, H2 reads
        // both forms, and the reference H2 2.5 was asked and takes "FROM "Rows" "r"" as readily as
        // "FROM "Rows" AS "r"". Of the two forms the bare one is the shorter, and the engine's
        // parser reads it as well.
        Sql.Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Name, tableExpression.Schema));

        if (tableExpression.Alias is { } alias)
        {
            Sql.Append(" ").Append(_sqlGenerationHelper.DelimitIdentifier(alias));
        }

        return tableExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitExtension(Expression extensionExpression)
        => extensionExpression switch
        {
            H2AllExpression e => VisitArrayAll(e),
            H2AnyExpression e => VisitArrayAny(e),
            H2ArrayIndexExpression e => VisitArrayIndex(e),
            H2ArraySliceExpression e => VisitArraySlice(e),
            H2BinaryExpression e => VisitPgBinary(e),
            H2DeleteExpression e => VisitH2Delete(e),
            H2FunctionExpression e => VisitPgFunction(e),
            H2ILikeExpression e => VisitILike(e),
            H2JsonTraversalExpression e => VisitJsonPathTraversal(e),
            H2NewArrayExpression e => VisitNewArray(e),
            H2RegexMatchExpression e => VisitRegexMatch(e),
            H2RowValueExpression e => VisitRowValue(e),
            H2UnknownBinaryExpression e => VisitUnknownBinary(e),
            H2TableValuedFunctionExpression e => VisitPgTableValuedFunctionExpression(e),

            _ => base.VisitExtension(extensionExpression)
        };

    /// <inheritdoc />
    protected override void GenerateRootCommand(Expression queryExpression)
    {
        switch (queryExpression)
        {
            case H2DeleteExpression h2DeleteExpression:
                GenerateTagsHeaderComment(h2DeleteExpression.Tags);
                VisitH2Delete(h2DeleteExpression);
                break;

            default:
                base.GenerateRootCommand(queryExpression);
                break;
        }
    }

    /// <inheritdoc />
    protected override void GenerateLimitOffset(SelectExpression selectExpression)
    {
        Check.NotNull(selectExpression, nameof(selectExpression));

        if (selectExpression.Limit is not null)
        {
            Sql.AppendLine().Append("LIMIT ");
            Visit(selectExpression.Limit);
        }

        if (selectExpression.Offset is not null)
        {
            if (selectExpression.Limit is null)
            {
                Sql.AppendLine();
            }
            else
            {
                Sql.Append(" ");
            }

            Sql.Append("OFFSET ");
            Visit(selectExpression.Offset);
        }
    }

    /// <inheritdoc />
    protected override string GetOperator(SqlBinaryExpression e)
        => e.OperatorType switch
        {
            // The strings and the arrays are joined with the functions of H2 2.5, CONCAT and
            // ARRAY_CAT, and the exclusive or is BITXOR: the pipes ("||") and the hash ("#")
            // are not in the grammar of H2 2.5. The three are written in VisitSqlBinary.
            ExpressionType.And when e.Type == typeof(bool) => " AND ",
            ExpressionType.Or when e.Type == typeof(bool) => " OR ",

            _ => base.GetOperator(e)
        };

    /// <inheritdoc />
    protected override Expression VisitOrdering(OrderingExpression ordering)
    {
        var result = base.VisitOrdering(ordering);

        if (_reverseNullOrderingEnabled)
        {
            Sql.Append(ordering.IsAscending ? " NULLS FIRST" : " NULLS LAST");
        }

        return result;
    }

    /// <inheritdoc />
    protected override void GenerateTop(SelectExpression selectExpression)
    {
        // H2 2.5 has no TOP(), see GenerateLimitOffset
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
    {
        // H2 2.5 has no LATERAL: the word is not in its grammar, and the reference refuses
        // "JOIN LATERAL" with a syntax error. A cross apply over a table can still be written
        // as a cross join with the condition in the where clause, but a cross apply over a
        // subquery that reads the row of the outer table cannot be written at all.
        throw new InvalidOperationException(
            "H2 2.5 has no LATERAL, so a cross apply cannot be written as SQL. See docs/DIVERGENCES.md.");
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
    {
        // H2 2.5 has no LATERAL: the word is not in its grammar, and the reference refuses
        // "LEFT JOIN LATERAL" with a syntax error.
        throw new InvalidOperationException(
            "H2 2.5 has no LATERAL, so an outer apply cannot be written as SQL. See docs/DIVERGENCES.md.");
    }

    /// <inheritdoc />
    protected override Expression VisitSqlBinary(SqlBinaryExpression binary)
    {
        switch (binary.OperatorType)
        {
            case ExpressionType.Add when binary.Type == typeof(string):
                // The strings are joined with CONCAT, the function H2 2.5 has. The pipes
                // ("a || b") mean something else: they make the whole result NULL when
                // one of the parts is NULL, while CONCAT ignores the NULL parts. The reference H2
                // 2.5 was asked about both: CONCAT('a', NULL) is 'a', and 'a' || NULL is NULL.
                Sql.Append("CONCAT(");
                Visit(binary.Left);
                Sql.Append(", ");
                Visit(binary.Right);
                Sql.Append(")");
                return binary;

            case ExpressionType.Add
                when binary.Left.TypeMapping is H2ArrayTypeMapping && binary.Right.TypeMapping is H2ArrayTypeMapping:
                // The arrays are joined with ARRAY_CAT, the function H2 2.5 has. The pipes
                // ("a || b") are not in the grammar of H2 2.5.
                Sql.Append("ARRAY_CAT(");
                Visit(binary.Left);
                Sql.Append(", ");
                Visit(binary.Right);
                Sql.Append(")");
                return binary;

            case ExpressionType.ExclusiveOr when binary.Type != typeof(bool):
                // The exclusive or is BITXOR, the function H2 2.5 has. The hash ("a # b") is
                // not in the grammar of H2 2.5.
                Sql.Append("BITXOR(");
                Visit(binary.Left);
                Sql.Append(", ");
                Visit(binary.Right);
                Sql.Append(")");
                return binary;

            case ExpressionType.ArrayIndex:
                return VisitArrayIndex(binary);

            default:
                return base.VisitSqlBinary(binary);
        }
    }

    // NonQueryConvertingExpressionVisitor converts the relational DeleteExpression to PostgresDeleteExpression, so we should never
    // get here
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitDelete(DeleteExpression deleteExpression)
        => throw new InvalidOperationException("Inconceivable!");

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <remarks>
    ///     H2's EXTRACT is a syntax and not a call of a function: EXTRACT(field FROM value). The
    ///     reference refuses the call -- "SELECT extract('epoch', x)" answers "expected "FROM"" --
    ///     and takes the syntax with the field in single quotes as well as with a bare word.
    /// </remarks>
    protected override Expression VisitSqlFunction(SqlFunctionExpression expression)
    {
        if (expression.Name.Equals("extract", StringComparison.OrdinalIgnoreCase)
            && expression.Arguments is { Count: 2 })
        {
            Sql.Append("EXTRACT(");
            Visit(expression.Arguments[0]);
            Sql.Append(" FROM ");
            Visit(expression.Arguments[1]);
            Sql.Append(")");

            return expression;
        }

        return base.VisitSqlFunction(expression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitH2Delete(H2DeleteExpression h2DeleteExpression)
    {
        Sql.Append("DELETE FROM ");
        Visit(h2DeleteExpression.Table);

        if (h2DeleteExpression.FromItems.Count > 0)
        {
            Sql.AppendLine().Append("USING ");
            GenerateList(h2DeleteExpression.FromItems, t => Visit(t), sql => sql.Append(", "));
        }

        if (h2DeleteExpression.Predicate != null)
        {
            Sql.AppendLine().Append("WHERE ");
            Visit(h2DeleteExpression.Predicate);
        }

        return h2DeleteExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitUpdate(UpdateExpression updateExpression)
    {
        var selectExpression = updateExpression.SelectExpression;

        if (selectExpression.Offset == null
            && selectExpression.Limit == null
            && selectExpression.Having == null
            && selectExpression.Orderings.Count == 0
            && selectExpression.GroupBy.Count == 0
            && selectExpression.Projection.Count == 0
            && (selectExpression.Tables.Count == 1
                || !ReferenceEquals(selectExpression.Tables[0], updateExpression.Table)
                || selectExpression.Tables[1] is InnerJoinExpression
                || selectExpression.Tables[1] is CrossJoinExpression))
        {
            Sql.Append("UPDATE ");
            Visit(updateExpression.Table);
            Sql.AppendLine();
            Sql.Append("SET ");
            Sql.Append(
                $"{_sqlGenerationHelper.DelimitIdentifier(updateExpression.ColumnValueSetters[0].Column.Name)} = ");
            Visit(updateExpression.ColumnValueSetters[0].Value);
            using (Sql.Indent())
            {
                foreach (var columnValueSetter in updateExpression.ColumnValueSetters.Skip(1))
                {
                    Sql.AppendLine(",");
                    Sql.Append($"{_sqlGenerationHelper.DelimitIdentifier(columnValueSetter.Column.Name)} = ");
                    Visit(columnValueSetter.Value);
                }
            }

            var predicate = selectExpression.Predicate;
            var firstTable = true;
            OuterReferenceFindingExpressionVisitor? visitor = null;

            if (selectExpression.Tables.Count > 1)
            {
                Sql.AppendLine().Append("FROM ");

                for (var i = 0; i < selectExpression.Tables.Count; i++)
                {
                    var table = selectExpression.Tables[i];
                    var joinExpression = table as JoinExpressionBase;

                    if (updateExpression.Table.Alias == (joinExpression?.Table.Alias ?? table.Alias))
                    {
                        LiftPredicate(table);
                        continue;
                    }

                    visitor ??= new OuterReferenceFindingExpressionVisitor(updateExpression.Table);

                    // The main update table cannot be referenced from anywhere except for the UPDATE WHERE clause.
                    // This specifically makes it impossible to have joins which reference the main table in their predicate (ON ...).
                    // Because of this, we detect all such inner joins and lift their predicates to the main WHERE clause (where a reference to the
                    // main table is allowed), producing UPDATE ... FROM x, y WHERE y.foreign_key = x.id instead of INNER JOIN ... ON.
                    if (firstTable)
                    {
                        LiftPredicate(table);
                        table = joinExpression?.Table ?? table;
                    }
                    else if (joinExpression is InnerJoinExpression innerJoinExpression
                             && visitor.ContainsReferenceToMainTable(innerJoinExpression.JoinPredicate))
                    {
                        LiftPredicate(innerJoinExpression);

                        Sql.AppendLine(",");
                        using (Sql.Indent())
                        {
                            Visit(innerJoinExpression.Table);
                        }

                        continue;
                    }

                    if (firstTable)
                    {
                        firstTable = false;
                    }
                    else
                    {
                        Sql.AppendLine();
                    }

                    Visit(table);

                    void LiftPredicate(TableExpressionBase joinTable)
                    {
                        if (joinTable is PredicateJoinExpressionBase predicateJoinExpression)
                        {
                            Check.DebugAssert(joinExpression is not LeftJoinExpression, "Cannot lift predicate for left join");

                            predicate = predicate == null
                                ? predicateJoinExpression.JoinPredicate
                                : new SqlBinaryExpression(
                                    ExpressionType.AndAlso,
                                    predicateJoinExpression.JoinPredicate,
                                    predicate,
                                    typeof(bool),
                                    predicate.TypeMapping);
                        }
                    }
                }
            }

            if (predicate != null)
            {
                Sql.AppendLine().Append("WHERE ");
                Visit(predicate);
            }

            return updateExpression;
        }

        throw new InvalidOperationException(
            RelationalStrings.ExecuteOperationWithUnsupportedOperatorInSqlGeneration(nameof(EntityFrameworkQueryableExtensions.ExecuteUpdate)));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitNewArray(H2NewArrayExpression h2NewArrayExpression)
    {
        Debug.Assert(h2NewArrayExpression.TypeMapping is not null);

        Sql.Append("ARRAY[");
        var first = true;
        foreach (var initializer in h2NewArrayExpression.Expressions)
        {
            if (!first)
            {
                Sql.Append(",");
            }

            first = false;
            Visit(initializer);
        }

        // No cast after the brackets: H2 2.5 says what the array holds without one
        // ("ARRAY[1,2]" is enough), and it has no such cast: the type of the array
        // follows from the elements and from the place the array stands in.
        return h2NewArrayExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitPgBinary(H2BinaryExpression binaryExpression)
    {
        Check.NotNull(binaryExpression, nameof(binaryExpression));

        var requiresParentheses = RequiresParentheses(binaryExpression, binaryExpression.Left);

        if (requiresParentheses)
        {
            Sql.Append("(");
        }

        Visit(binaryExpression.Left);

        if (requiresParentheses)
        {
            Sql.Append(")");
        }

        Debug.Assert(binaryExpression.Left.TypeMapping is not null);
        Debug.Assert(binaryExpression.Right.TypeMapping is not null);

        Sql
            .Append(" ")
            .Append(
                binaryExpression.OperatorType switch
                {
                    H2ExpressionType.Contains
                        => ">>",

                    H2ExpressionType.ContainedBy
                        => "<<",

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
                        when binaryExpression.Right.TypeMapping.StoreType == "lquery"
                        || binaryExpression.Right.TypeMapping is H2ArrayTypeMapping { ElementTypeMapping.StoreType: "lquery" } => "~",
                    H2ExpressionType.LTreeMatches
                        when binaryExpression.Right.TypeMapping.StoreType == "ltxtquery"
                        => "@",
                    H2ExpressionType.LTreeMatchesAny => "?",
                    H2ExpressionType.LTreeFirstAncestor => "?@>",
                    H2ExpressionType.LTreeFirstDescendent => "?<@",
                    H2ExpressionType.LTreeFirstMatches
                        when binaryExpression.Right.TypeMapping.StoreType == "lquery" => "?~",
                    H2ExpressionType.LTreeFirstMatches
                        when binaryExpression.Right.TypeMapping.StoreType == "ltxtquery" => "?@",

                    H2ExpressionType.Distance => "<->",

                    _ => throw new ArgumentOutOfRangeException($"Unhandled operator type: {binaryExpression.OperatorType}")
                })
            .Append(" ");

        requiresParentheses = RequiresParentheses(binaryExpression, binaryExpression.Right);

        if (requiresParentheses)
        {
            Sql.Append("(");
        }

        Visit(binaryExpression.Right);

        if (requiresParentheses)
        {
            Sql.Append(")");
        }

        return binaryExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitArrayIndex(SqlBinaryExpression expression)
    {
        Visit(expression.Left);
        Sql.Append("[");
        Visit(expression.Right);
        Sql.Append("]");
        return expression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
    {
        Debug.Assert(sqlUnaryExpression.TypeMapping is not null);
        Debug.Assert(sqlUnaryExpression.Operand.TypeMapping is not null);

        switch (sqlUnaryExpression.OperatorType)
        {
            case ExpressionType.Convert:
            {
                // H2 2.5 has no x::y: that shorter cast is not in its grammar, and the reference
                // refuses it. The standard CAST(x AS y) is what H2 takes, and the type in it
                // is the type of H2 -- "integer".
                Sql.Append("CAST(");
                Visit(sqlUnaryExpression.Operand);
                Sql.Append(" AS ");
                Sql.Append(sqlUnaryExpression.TypeMapping.StoreType);
                Sql.Append(")");
                return sqlUnaryExpression;
            }

            // Bitwise complement on networking types
            case ExpressionType.Not when
                sqlUnaryExpression.Operand.TypeMapping.ClrType == typeof(IPAddress)
                || sqlUnaryExpression.Operand.TypeMapping.ClrType == typeof((IPAddress, int))
                || sqlUnaryExpression.Operand.TypeMapping.ClrType == typeof(PhysicalAddress):
                Sql.Append("~");
                Visit(sqlUnaryExpression.Operand);
                return sqlUnaryExpression;

            // NOT over expression types which have fancy embedded negation
            case ExpressionType.Not
                when sqlUnaryExpression.Type == typeof(bool):
            {
                switch (sqlUnaryExpression.Operand)
                {
                    case H2RegexMatchExpression regexMatch:
                        VisitRegexMatch(regexMatch, negated: true);
                        return sqlUnaryExpression;

                    case H2ILikeExpression iLike:
                        VisitILike(iLike, negated: true);
                        return sqlUnaryExpression;
                }

                break;
            }
        }

        return base.VisitSqlUnary(sqlUnaryExpression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateSetOperationOperand(SetOperationBase setOperation, SelectExpression operand)
    {
        // The operands of a set operation are written in parentheses when they carry their own
        // ORDER BY or LIMIT
        if (operand.Orderings.Count > 0 || operand.Limit is not null)
        {
            Sql.AppendLine("(");
            using (Sql.Indent())
            {
                Visit(operand);
            }

            Sql.AppendLine().Append(")");
            return;
        }

        base.GenerateSetOperationOperand(setOperation, operand);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitCollate(CollateExpression collateExpression)
    {
        Check.NotNull(collateExpression, nameof(collateExpression));

        Visit(collateExpression.Operand);

        // H2 2.5 has no collation of an expression: the reference reads "COLLATE" only in the
        // compatibility mode.
        throw new NotSupportedException(
            "H2 2.5 has no collation of an expression, so it cannot be written as SQL.");
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool TryGenerateWithoutWrappingSelect(SelectExpression selectExpression)
        // H2 2.5 supports VALUES as a top-level statement - and directly under set operations.
        // However, when on the left side of a set operation, we need the column coming out of VALUES to be named, so we need the wrapping
        // SELECT for that.
        => selectExpression.Tables is not [ValuesExpression]
            && base.TryGenerateWithoutWrappingSelect(selectExpression);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateSetOperation(SetOperationBase setOperation)
    {
        GenerateSetOperationOperand(setOperation, setOperation.Source1);

        Sql
            .AppendLine()
            .Append(
                setOperation switch
                {
                    ExceptExpression => "EXCEPT",
                    IntersectExpression => "INTERSECT",
                    UnionExpression => "UNION",
                    _ => throw new InvalidOperationException(CoreStrings.UnknownEntity("SetOperationType"))
                })
            .AppendLine(setOperation.IsDistinct ? string.Empty : " ALL");

        // For ValuesExpression, we can remove its wrapping SelectExpression but only if on the right side of a set operation, since on
        // the left side we need the column name to be specified.
        if (setOperation.Source2 is
            {
                Tables: [ValuesExpression valuesExpression],
                Offset: null,
                Limit: null,
                IsDistinct: false,
                Predicate: null,
                Having: null,
                Orderings.Count: 0,
                GroupBy.Count: 0,
            } rightSelectExpression
            && rightSelectExpression.Projection.Count == valuesExpression.ColumnNames.Count
            && rightSelectExpression.Projection.Select(
                    (pe, index) => pe.Expression is ColumnExpression column
                        && column.Name == valuesExpression.ColumnNames[index])
                .All(e => e))
        {
            GenerateValues(valuesExpression);
        }
        else
        {
            GenerateSetOperationOperand(setOperation, setOperation.Source2);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitValues(ValuesExpression valuesExpression)
    {
        base.VisitValues(valuesExpression);

        // H2 2.5 supports setting the projects column names: FROM (VALUES (1), (2)) AS v(foo)
        Sql.Append("(");

        for (var i = 0; i < valuesExpression.ColumnNames.Count; i++)
        {
            if (i > 0)
            {
                Sql.Append(", ");
            }

            Sql.Append(_sqlGenerationHelper.DelimitIdentifier(valuesExpression.ColumnNames[i]));
        }

        Sql.Append(")");

        return valuesExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void GenerateValues(ValuesExpression valuesExpression)
    {
        if (valuesExpression.RowValues is null)
        {
            throw new UnreachableException();
        }

        if (valuesExpression.RowValues.Count == 0)
        {
            throw new InvalidOperationException(RelationalStrings.EmptyCollectionNotSupportedAsInlineQueryRoot);
        }

        // H2 2.5 supports providing the names of columns projected out of VALUES: (VALUES (1, 3), (2, 4)) AS x(a, b).
        // But since other databases sometimes don't, the default relational implementation is complex, involving a SELECT for the first row
        // and a UNION All on the rest. Override to do the nice simple thing.
        var rowValues = valuesExpression.RowValues;

        Sql.Append("VALUES ");

        for (var i = 0; i < rowValues.Count; i++)
        {
            // TODO: Do we want newlines here?
            if (i > 0)
            {
                Sql.Append(", ");
            }

            Visit(valuesExpression.RowValues[i]);
        }
    }

    #region PostgreSQL-specific expression types

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitArrayAll(H2AllExpression expression)
    {
        Visit(expression.Item);

        Sql
            .Append(" ")
            .Append(
                expression.OperatorType switch
                {
                    H2AllOperatorType.Like => "LIKE",
                    H2AllOperatorType.ILike => "ILIKE",
                    _ => throw new ArgumentOutOfRangeException($"Unhandled operator type: {expression.OperatorType}")
                })
            .Append(" ALL (");

        Visit(expression.Array);

        Sql.Append(")");

        return expression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitArrayAny(H2AnyExpression expression)
    {
        Visit(expression.Item);

        Sql
            .Append(" ")
            .Append(
                expression.OperatorType switch
                {
                    H2AnyOperatorType.Equal => "=",
                    H2AnyOperatorType.Like => "LIKE",
                    H2AnyOperatorType.ILike => "ILIKE",
                    _ => throw new ArgumentOutOfRangeException($"Unhandled operator type: {expression.OperatorType}")
                })
            .Append(" ANY (");

        Visit(expression.Array);

        Sql.Append(")");

        return expression;
    }

    /// <summary>
    ///     Produces SQL array index expression (e.g. arr[1]).
    /// </summary>
    protected virtual Expression VisitArrayIndex(H2ArrayIndexExpression expression)
    {
        var requiresParentheses = RequiresParentheses(expression, expression.Array);

        if (requiresParentheses)
        {
            Sql.Append("(");
        }

        Visit(expression.Array);

        if (requiresParentheses)
        {
            Sql.Append(")");
        }

        Sql.Append("[");
        Visit(expression.Index);
        Sql.Append("]");
        return expression;
    }

    /// <summary>
    ///     Produces SQL array slice expression (e.g. arr[1:2]).
    /// </summary>
    protected virtual Expression VisitArraySlice(H2ArraySliceExpression expression)
    {
        // A slice of an array is a function in H2 2.5 and not a piece of syntax: the reference
        // refuses "arr[1:2]" ("expected "[, ., ::, AT, FORMAT, *, /, %, +, -, ||, NOT, IS,
        // ILIKE, REGEXP, AND, OR, ]"") and takes ARRAY_SLICE(arr, 1, 2), which answers the same
        // "[10, 20]" for the array [10, 20, 30]. The positions are counted from one in both, and
        // the ends are included in both.
        Sql.Append("ARRAY_SLICE(");
        Visit(expression.Array);
        Sql.Append(", ");

        // An open end is the first or the last element. H2's function takes both ends, and a
        // missing one is NULL there ("ARRAY_SLICE" answers NULL for a NULL bound), so the open
        // ends are written out: the first element is 1, the last is the size of the array.
        if (expression.LowerBound is null)
        {
            Sql.Append("1");
        }
        else
        {
            Visit(expression.LowerBound);
        }

        Sql.Append(", ");

        if (expression.UpperBound is null)
        {
            Sql.Append("CARDINALITY(");
            Visit(expression.Array);
            Sql.Append(")");
        }
        else
        {
            Visit(expression.UpperBound);
        }

        Sql.Append(")");

        return expression;
    }

    /// <summary>
    ///     Produces SQL for the regex matching of H2 2.5.
    /// </summary>
    /// <remarks>
    ///     The functions of H2 2.5 are in the frozen documentation of the engine:
    ///     <c>../h2sharp/docs/functions.html</c>.
    /// </remarks>
    protected virtual Expression VisitRegexMatch(H2RegexMatchExpression expression, bool negated = false)
    {
        var options = expression.Options;

        Visit(expression.Match);

        if (options.HasFlag(RegexOptions.IgnoreCase))
        {
            Sql.Append(negated ? " !~* " : " ~* ");
            options &= ~RegexOptions.IgnoreCase;
        }
        else
        {
            Sql.Append(negated ? " !~ " : " ~ ");
        }

        // PG regexps are single-line by default
        if (options == RegexOptions.Singleline)
        {
            Visit(expression.Pattern);
            return expression;
        }

        var constantPattern = (expression.Pattern as SqlConstantExpression)?.Value as string;

        if (constantPattern is null)
        {
            Sql.Append("(");
        }

        Sql.Append("'(?");

        if (options.HasFlag(RegexOptions.Multiline))
        {
            Sql.Append("n");
        }
        else if (!options.HasFlag(RegexOptions.Singleline))
        {
            // In .NET's default mode, . doesn't match newlines, and the pattern of H2 2.5 is
            // read as it is written: the flag says that the newlines are not a part of it.
            Sql.Append("p");
        }

        if (options.HasFlag(RegexOptions.IgnorePatternWhitespace))
        {
            Sql.Append("x");
        }

        Sql.Append(")");

        if (constantPattern is null)
        {
            Sql.Append("' || ");
            Visit(expression.Pattern);
            Sql.Append(")");
        }
        else
        {
            Sql.Append(constantPattern.Replace("'", "''"));
            Sql.Append("'");
        }

        // Sql.Append(")' || ");
        // Visit(expression.Pattern);
        // Sql.Append(")");

        return expression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitRowValue(H2RowValueExpression rowValueExpression)
    {
        Sql.Append("(");

        var values = rowValueExpression.Values;
        var count = values.Count;
        for (var i = 0; i < count; i++)
        {
            Visit(values[i]);

            if (i < count - 1)
            {
                Sql.Append(", ");
            }
        }

        Sql.Append(")");

        return rowValueExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitILike(H2ILikeExpression likeExpression, bool negated = false)
    {
        Visit(likeExpression.Match);

        if (negated)
        {
            Sql.Append(" NOT");
        }

        Sql.Append(" ILIKE ");

        Visit(likeExpression.Pattern);

        if (likeExpression.EscapeChar is not null)
        {
            Sql.Append(" ESCAPE ");
            Visit(likeExpression.EscapeChar);
        }

        return likeExpression;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override Expression VisitJsonScalar(JsonScalarExpression jsonScalarExpression)
    {
        // TODO: Stop producing empty JsonScalarExpressions, #30768
        var path = jsonScalarExpression.Path;
        if (path.Count == 0)
        {
            Visit(jsonScalarExpression.Json);
            return jsonScalarExpression;
        }

        switch (jsonScalarExpression.TypeMapping)
        {
            // This case is for when a nested JSON entity is being accessed. We want the json/jsonb fragment in this case (not text),
            // so we can perform further JSON operations on it.
            case H2OwnedJsonTypeMapping:
                GenerateJsonPath(returnsText: false);
                break;

            // No need to cast the output when we expect a string anyway
            case StringTypeMapping:
                GenerateJsonPath(returnsText: true);
                break;

            // bytea requires special handling, since we encode the binary data as base64 inside the JSON, but that requires a special
            // conversion function to be extracted out to a PG bytea.
            case H2ByteArrayTypeMapping:
                Sql.Append("decode(");
                GenerateJsonPath(returnsText: true);
                Sql.Append(", 'base64')");
                break;

            // Arrays require special handling; we cannot simply cast a JSON array (as text) to a PG array ([1,2,3] isn't a valid PG array
            // representation). We use jsonb_array_elements_text to extract the array elements as a set, cast them to their PG element type
            // and then build an array from that.
            case H2ArrayTypeMapping arrayMapping:
                Sql.Append("(ARRAY(SELECT CAST(element AS ").Append(arrayMapping.ElementTypeMapping.StoreType)
                    .Append(") FROM jsonb_array_elements_text(");
                GenerateJsonPath(returnsText: false);
                Sql.Append(") WITH ORDINALITY AS t(element) ORDER BY ordinality))");
                break;

            default:
                Sql.Append("CAST(");
                GenerateJsonPath(returnsText: true);
                Sql.Append(" AS ");
                Sql.Append(jsonScalarExpression.TypeMapping!.StoreType);
                Sql.Append(")");
                break;
        }

        return jsonScalarExpression;

        void GenerateJsonPath(bool returnsText)
            => this.GenerateJsonPath(
                jsonScalarExpression.Json,
                returnsText: returnsText,
                jsonScalarExpression.Path.Select(
                    s => s switch
                    {
                        { PropertyName: string propertyName }
                            => new SqlConstantExpression(propertyName, _textTypeMapping ??= _typeMappingSource.FindMapping(typeof(string))),
                        { ArrayIndex: SqlExpression arrayIndex } => arrayIndex,
                        _ => throw new UnreachableException()
                    }).ToList());
    }

    /// <summary>
    ///     Visits the children of an <see cref="H2JsonTraversalExpression" />.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>
    ///     An <see cref="Expression" />.
    /// </returns>
    protected virtual Expression VisitJsonPathTraversal(H2JsonTraversalExpression expression)
    {
        GenerateJsonPath(expression.Expression, expression.ReturnsText, expression.Path);
        return expression;
    }

    private void GenerateJsonPath(SqlExpression expression, bool returnsText, IReadOnlyList<SqlExpression> path)
    {
        Visit(expression);

        if (path.Count == 1)
        {
            Sql.Append(returnsText ? " ->> " : " -> ");
            Visit(path[0]);
            return;
        }

        // Multiple path components
        Sql.Append(returnsText ? " #>> " : " #> ");

        // Use simplified array literal syntax if all path components are constants for cleaner SQL
        if (path.All(p => p is SqlConstantExpression { Value: var pathSegment }
                && (pathSegment is not string s || s.All(char.IsAsciiLetterOrDigit))))
        {
            Sql
                .Append("'{")
                .Append(string.Join(",", path.Select(p => ((SqlConstantExpression)p).Value)))
                .Append("}'");
        }
        else
        {
            Sql.Append("ARRAY[");
            for (var i = 0; i < path.Count; i++)
            {
                Visit(path[i]);
                if (i < path.Count - 1)
                {
                    Sql.Append(",");
                }
            }

            // No cast after the brackets: H2 2.5 has no "::" and the type of the array
            // follows from the elements and from the place the array stands in.
            Sql.Append("]");
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitPgTableValuedFunctionExpression(H2TableValuedFunctionExpression tableValuedFunctionExpression)
    {
        // The elements of an array as rows are not a thing H2 2.5 can say: there is no UNNEST,
        // and no LATERAL either. The reference reads the elements of an array with a range
        // table and the element at a place:
        //
        //   SELECT t.id, r.x, ARRAY_GET(t.arr, r.x)
        //     FROM t, SYSTEM_RANGE(1, 65536) r
        //    WHERE r.x <= ARRAY_LENGTH(t.arr)
        //
        // The element there is an expression over the row of the table, and not a column of a
        // table that is joined -- and the model EF has of a collection is exactly a table whose
        // column is the element. So the form H2 has cannot be written in the model EF has, and
        // the table of the elements is refused rather than written in the words of another
        // dialect: "unnest(arr) WITH ORDINALITY" the reference refuses ("expected
        // "statement"").
        if (tableValuedFunctionExpression is H2UnnestExpression)
        {
            throw new InvalidOperationException(
                "H2 2.5 has no UNNEST and no LATERAL, so the elements of an array cannot be rows here. See docs/DIVERGENCES.md.");
        }

        // H2TableValuedFunctionExpression extends the standard TableValuedFunctionExpression, adding the possibility to specify
        // column names and types at the end, as well as an optional WITH ORDINALITY to project an index out.

        // Note that a column definition (with type) *and* WITH ORDINALITY cannot be named
        // together; but the function invocation and the column definition can be wrapped
        // inside ROWS FROM, with the table alias and WITH ORDINALITY outside.
        // We take care of that here.
        if (tableValuedFunctionExpression is
            {
                WithOrdinality: true,
                ColumnInfos: { } columnInfos
            }
            && columnInfos.Any(ci => ci.TypeMapping is not null))
        {
            Sql.Append("ROWS FROM (");

            Sql.Append(tableValuedFunctionExpression.Name).Append("(");
            GenerateList(tableValuedFunctionExpression.Arguments, e => Visit(e));
            Sql.Append(") AS ");

            GenerateColumnDefinition();

            Sql.Append(") WITH ORDINALITY AS ").Append(_sqlGenerationHelper.DelimitIdentifier(tableValuedFunctionExpression.Alias));
        }
        else
        {
            Sql.Append(tableValuedFunctionExpression.Name).Append("(");
            GenerateList(tableValuedFunctionExpression.Arguments, e => Visit(e));
            Sql.Append(")");

            if (tableValuedFunctionExpression.WithOrdinality)
            {
                Sql.Append(" WITH ORDINALITY");
            }

            Sql.Append(AliasSeparator).Append(_sqlGenerationHelper.DelimitIdentifier(tableValuedFunctionExpression.Alias));

            if (tableValuedFunctionExpression.ColumnInfos is not null)
            {
                GenerateColumnDefinition();
            }
        }

        return tableValuedFunctionExpression;

        void GenerateColumnDefinition()
        {
            Sql.Append("(");

            if (tableValuedFunctionExpression.ColumnInfos is [var singleColumnInfo])
            {
                GenerateColumnInfo(singleColumnInfo);
            }
            else
            {
                Sql.AppendLine();
                using var _ = Sql.Indent();

                for (var i = 0; i < tableValuedFunctionExpression.ColumnInfos.Count; i++)
                {
                    var columnInfo = tableValuedFunctionExpression.ColumnInfos[i];

                    if (i > 0)
                    {
                        Sql.AppendLine(",");
                    }

                    GenerateColumnInfo(columnInfo);
                }

                Sql.AppendLine();
            }

            Sql.Append(")");

            void GenerateColumnInfo(H2TableValuedFunctionExpression.ColumnInfo columnInfo)
            {
                Sql.Append(_sqlGenerationHelper.DelimitIdentifier(columnInfo.Name));

                if (columnInfo.TypeMapping is not null)
                {
                    Sql.Append(" ").Append(columnInfo.TypeMapping.StoreType);
                }
            }
        }
    }

    /// <summary>
    ///     Visits the children of a <see cref="H2UnknownBinaryExpression" />.
    /// </summary>
    /// <param name="unknownBinaryExpression">The expression.</param>
    /// <returns>
    ///     An <see cref="Expression" />.
    /// </returns>
    protected virtual Expression VisitUnknownBinary(H2UnknownBinaryExpression unknownBinaryExpression)
    {
        Check.NotNull(unknownBinaryExpression, nameof(unknownBinaryExpression));

        var requiresParentheses = RequiresParentheses(unknownBinaryExpression, unknownBinaryExpression.Left);

        if (requiresParentheses)
        {
            Sql.Append("(");
        }

        Visit(unknownBinaryExpression.Left);

        if (requiresParentheses)
        {
            Sql.Append(")");
        }

        Sql
            .Append(" ")
            .Append(unknownBinaryExpression.Operator)
            .Append(" ");

        requiresParentheses = RequiresParentheses(unknownBinaryExpression, unknownBinaryExpression.Right);

        if (requiresParentheses)
        {
            Sql.Append("(");
        }

        Visit(unknownBinaryExpression.Right);

        if (requiresParentheses)
        {
            Sql.Append(")");
        }

        return unknownBinaryExpression;
    }

    /// <summary>
    ///     Visits the children of a <see cref="H2FunctionExpression" />.
    /// </summary>
    /// <param name="e">The expression.</param>
    /// <returns>
    ///     An <see cref="Expression" />.
    /// </returns>
    protected virtual Expression VisitPgFunction(H2FunctionExpression e)
    {
        Check.NotNull(e, nameof(e));

        if (e.IsBuiltIn)
        {
            Sql.Append(e.Name);
        }
        else
        {
            if (!string.IsNullOrEmpty(e.Schema))
            {
                Sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(e.Schema))
                    .Append(".");
            }

            // TODO: Quote user-defined function names with upper-case (also for regular SqlFunctionExpression)

            Sql.Append(_sqlGenerationHelper.DelimitIdentifier(e.Name));
        }

        Sql.Append("(");

        if (e.IsAggregateDistinct)
        {
            Sql.Append("DISTINCT ");
        }

        for (var i = 0; i < e.Arguments.Count; i++)
        {
            if (i < e.ArgumentNames.Count && e.ArgumentNames[i] is { } argumentName)
            {
                Sql
                    .Append(argumentName)
                    .Append(" => ");
            }

            Visit(e.Arguments[i]);

            if (i < e.Arguments.Count - 1)
            {
                Sql.Append(
                    i < e.ArgumentSeparators.Count && e.ArgumentSeparators[i] is not null
                        ? $" {e.ArgumentSeparators[i]} "
                        : ", ");
            }
        }

        if (e.AggregateOrderings.Count > 0 && TakesOrderInsideParentheses(e.Name))
        {
            Sql.Append(" ORDER BY ");

            for (var i = 0; i < e.AggregateOrderings.Count; i++)
            {
                if (i > 0)
                {
                    Sql.Append(", ");
                }

                Visit(e.AggregateOrderings[i]);
            }
        }

        Sql.Append(")");

        if (e.AggregateOrderings.Count > 0 && !TakesOrderInsideParentheses(e.Name))
        {
            if (!TakesOrderInWithinGroup(e.Name))
            {
                throw new InvalidOperationException(
                    $"H2 2.5 has no place for the order of the rows of the aggregate '{e.Name}': " +
                    "the order of the string aggregate goes into a WITHIN GROUP clause, and the " +
                    "aggregate of arrays takes it inside the parentheses. See docs/DIVERGENCES.md.");
            }

            // WITHIN GROUP (ORDER BY ...): the order of the rows of the string aggregate.
            Sql.Append(" WITHIN GROUP (ORDER BY ");

            for (var i = 0; i < e.AggregateOrderings.Count; i++)
            {
                if (i > 0)
                {
                    Sql.Append(", ");
                }

                Visit(e.AggregateOrderings[i]);
            }

            Sql.Append(")");
        }

        if (e.AggregatePredicate is not null)
        {
            // FILTER (WHERE ...): the filter of the rows of the aggregate, and it comes after
            // everything else: "LISTAGG(x, ', ') WITHIN GROUP (ORDER BY y) FILTER (WHERE z)".
            Sql.Append(" FILTER (WHERE ");

            Visit(e.AggregatePredicate);

            Sql.Append(")");
        }

        return e;
    }

    /// <summary>
    ///     Whether the aggregate takes the order of the rows inside its parentheses.
    /// </summary>
    /// <param name="name">The name of the aggregate function.</param>
    /// <returns>True for the aggregates that take it that way in H2 2.5.</returns>
    private static bool TakesOrderInsideParentheses(string name)
        => name.Equals("ARRAY_AGG", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Whether the aggregate takes the order of the rows in a WITHIN GROUP clause.
    /// </summary>
    /// <param name="name">The name of the aggregate function.</param>
    /// <returns>True for the aggregates that take it that way in H2 2.5.</returns>
    private static bool TakesOrderInWithinGroup(string name)
        => name.Equals("LISTAGG", StringComparison.OrdinalIgnoreCase);

    #endregion PostgreSQL-specific expression types

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool RequiresParentheses(SqlExpression outerExpression, SqlExpression innerExpression)
    {
        switch (innerExpression)
        {
            // PG doesn't support ~-, -~, ~~, -- so we add parentheses
            case SqlUnaryExpression innerUnary when outerExpression is SqlUnaryExpression outerUnary
                && (innerUnary.OperatorType is ExpressionType.Negate
                    || innerUnary.OperatorType is ExpressionType.Not && innerUnary.Type != typeof(bool))
                && (outerUnary.OperatorType is ExpressionType.Negate
                    || outerUnary.OperatorType is ExpressionType.Not && outerUnary.Type != typeof(bool)):
                return true;

            // Copy paste of QuerySqlGenerator.RequiresParentheses for SqlBinaryExpression
            case H2BinaryExpression innerBinary:
            {
                // If the provider defined precedence for the two expression, use that
                if (TryGetOperatorInfo(outerExpression, out var outerPrecedence, out var isOuterAssociative)
                    && TryGetOperatorInfo(innerExpression, out var innerPrecedence, out _))
                {
                    return outerPrecedence.CompareTo(innerPrecedence) switch
                    {
                        > 0 => true,
                        < 0 => false,

                        // If both operators have the same precedence, add parentheses unless they're the same operator, and
                        // that operator is associative (e.g. a + b + c)
                        _ => outerExpression is not H2BinaryExpression outerBinary
                            || outerBinary.OperatorType != innerBinary.OperatorType
                            || !isOuterAssociative
                            // Arithmetic operators on floating points aren't associative, because of rounding errors.
                            || outerExpression.Type == typeof(float)
                            || outerExpression.Type == typeof(double)
                            || innerExpression.Type == typeof(float)
                            || innerExpression.Type == typeof(double)
                    };
                }

                // Otherwise always parenthesize for safety
                return true;
            }

            case H2UnknownBinaryExpression:
                return true;

            default:
                return base.RequiresParentheses(outerExpression, innerExpression);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool TryGetOperatorInfo(SqlExpression expression, out int precedence, out bool isAssociative)
    {
        // The precedence of the operations is the one of H2 2.5: the frozen grammar of the engine
        // says it in ../h2sharp/docs/grammar.html
        (precedence, isAssociative) = expression switch
        {
            // TODO: Exponent => 1300

            SqlBinaryExpression sqlBinaryExpression => sqlBinaryExpression.OperatorType switch
            {
                // Multiplication, division, modulo
                ExpressionType.Multiply => (1200, true),
                ExpressionType.Divide => (1200, false),
                ExpressionType.Modulo => (1200, false),

                // Addition, subtraction (binary)
                ExpressionType.Add => (1100, true),
                ExpressionType.Subtract => (1100, false),

                // All other native and user-defined operators => 1000
                ExpressionType.LeftShift => (1000, true),
                ExpressionType.RightShift => (1000, true),
                ExpressionType.And when sqlBinaryExpression.Type != typeof(bool) => (1000, true),
                ExpressionType.Or when sqlBinaryExpression.Type != typeof(bool) => (1000, true),

                // Comparison operators
                ExpressionType.Equal => (800, false),
                ExpressionType.NotEqual => (800, false),
                ExpressionType.LessThan => (800, false),
                ExpressionType.LessThanOrEqual => (800, false),
                ExpressionType.GreaterThan => (800, false),
                ExpressionType.GreaterThanOrEqual => (800, false),

                // Logical operators
                ExpressionType.AndAlso => (500, true),
                ExpressionType.OrElse => (500, true),
                ExpressionType.And when sqlBinaryExpression.Type == typeof(bool) => (500, true),
                ExpressionType.Or when sqlBinaryExpression.Type == typeof(bool) => (500, true),

                _ => default,
            },

            SqlUnaryExpression sqlUnaryExpression => sqlUnaryExpression.OperatorType switch
            {
                ExpressionType.Convert => (1600, false),
                ExpressionType.Negate => (1400, false),
                ExpressionType.Not when sqlUnaryExpression.Type != typeof(bool) => (1000, false),
                ExpressionType.Equal => (700, false), // IS NULL
                ExpressionType.NotEqual => (700, false), // IS NOT NULL
                ExpressionType.Not when sqlUnaryExpression.Type == typeof(bool) => (600, false),

                _ => default,
            },

            // There's an "any other operator" category in the PG operator precedence table, we assign that a numeric value of 1000.
            // TODO: Some operators here may be associative
            H2BinaryExpression => (1000, false),

            CollateExpression => (1000, false),
            AtTimeZoneExpression => (1100, false),
            InExpression => (900, false),
            H2JsonTraversalExpression => (1000, false),
            H2ArrayIndexExpression => (1500, false),
            H2AllExpression or H2AnyExpression => (800, false),
            LikeExpression or H2ILikeExpression or H2RegexMatchExpression => (900, false),

            _ => default,
        };

        return precedence != default;
    }

    private void GenerateList<T>(
        IReadOnlyList<T> items,
        Action<T> generationAction,
        Action<IRelationalCommandBuilder>? joinAction = null)
    {
        joinAction ??= (isb => isb.Append(", "));

        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                joinAction(Sql);
            }

            generationAction(items[i]);
        }
    }

    private sealed class OuterReferenceFindingExpressionVisitor(TableExpression mainTable) : ExpressionVisitor
    {
        private bool _containsReference;

        public bool ContainsReferenceToMainTable(SqlExpression sqlExpression)
        {
            _containsReference = false;

            Visit(sqlExpression);

            return _containsReference;
        }

        [return: NotNullIfNotNull("expression")]
        public override Expression? Visit(Expression? expression)
        {
            if (_containsReference)
            {
                return expression;
            }

            if (expression is ColumnExpression { TableAlias: var tableAlias }
                && tableAlias == mainTable.Alias)
            {
                _containsReference = true;

                return expression;
            }

            return base.Visit(expression);
        }
    }
}
