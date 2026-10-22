using H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;

namespace H2Sharp.EntityFrameworkCore.Query.Internal;

/// <summary>
///     Converts the relational <see cref="NonQueryExpression" /> into an <see cref="H2DeleteExpression" />.
///     Joined sources are translated with a key subquery before reaching SQL generation.
/// </summary>
public class H2DeleteConvertingExpressionVisitor : ExpressionVisitor
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Expression Process(Expression node)
        => node switch
        {
            DeleteExpression deleteExpression => VisitDelete(deleteExpression),

            _ => node
        };

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual Expression VisitDelete(DeleteExpression deleteExpression)
    {
        var selectExpression = deleteExpression.SelectExpression;

        if (selectExpression.Offset != null
            || selectExpression.Limit != null
            || selectExpression.Having != null
            || selectExpression.Orderings.Count > 0
            || selectExpression.GroupBy.Count > 0
            || selectExpression.Projection.Count > 0)
        {
            throw new InvalidOperationException(
                RelationalStrings.ExecuteOperationWithUnsupportedOperatorInSqlGeneration(
                    nameof(EntityFrameworkQueryableExtensions.ExecuteDelete)));
        }

        var fromItems = new List<TableExpressionBase>();

        // The SelectExpression also contains the target table being modified (same as deleteExpression.Table).
        // Only single-table DELETE expressions reach this conversion. Joined sources must use
        // the relational key-subquery fallback, because H2 does not accept DELETE USING.
        // Note that the non-join TableExpression isn't necessarily the target table - through projection the last table being
        // joined may be the one being modified.
        foreach (var tableBase in selectExpression.Tables)
        {
            switch (tableBase)
            {
                case TableExpression tableExpression:
                    if (tableExpression.Alias != deleteExpression.Table.Alias)
                    {
                        throw new NotSupportedException(
                            "H2 2.5 cannot delete with other tables in the statement: the USING clause is PostgreSQL's, "
                            + "and the reference rejects it. Restate the delete with a predicate over a subquery instead.");
                    }

                    break;

                case InnerJoinExpression:
                    throw new NotSupportedException(
                        "H2 2.5 cannot delete over a join: the USING clause is PostgreSQL's, and the reference rejects it. "
                        + "Restate the delete with a predicate over a subquery instead.");

                default:
                    throw new InvalidOperationException(
                        RelationalStrings.ExecuteOperationWithUnsupportedOperatorInSqlGeneration(
                            nameof(EntityFrameworkQueryableExtensions.ExecuteDelete)));
            }
        }

        // With no joins the predicate stands on its own.
        var predicate = selectExpression.Predicate;

        return new H2DeleteExpression(deleteExpression.Table, fromItems, predicate, deleteExpression.Tags);
    }
}
