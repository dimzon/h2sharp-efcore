using System.Data;
using System.Text;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

namespace H2Sharp.EntityFrameworkCore.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2UpdateSqlGenerator : UpdateSqlGenerator
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2UpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override ResultSetMapping AppendInsertOperation(
        StringBuilder commandStringBuilder,
        IReadOnlyModificationCommand command,
        int commandPosition,
        out bool requiresTransaction)
        => AppendInsertOperation(commandStringBuilder, command, commandPosition, overridingSystemValue: false, out requiresTransaction);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ResultSetMapping AppendInsertOperation(
        StringBuilder commandStringBuilder,
        IReadOnlyModificationCommand command,
        int commandPosition,
        bool overridingSystemValue,
        out bool requiresTransaction)
    {
        var name = command.TableName;
        var schema = command.Schema;
        var operations = command.ColumnModifications;

        var writeOperations = operations.Where(o => o.IsWrite).ToList();
        var readOperations = operations.Where(o => o.IsRead).ToList();

        AppendInsertCommand(commandStringBuilder, name, schema, writeOperations, readOperations, overridingSystemValue);

        requiresTransaction = false;

        return readOperations.Count > 0 ? ResultSetMapping.LastInResultSet : ResultSetMapping.NoResults;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when upgrading to a new Entity Framework Core release.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         H2 2.5 has no <c>RETURNING</c>. The reference was asked, and it refuses the clause: the
    ///         row an insert made is read through <c>FINAL TABLE</c>, which is the form the H2 grammar
    ///         has for it (<c>{ OLD | NEW | FINAL } TABLE</c>).
    ///     </para>
    ///     <para>
    ///         The upstream base class appends <c>OUTPUT INSERTED.[...]</c>, which is neither H2's nor
    ///         PostgreSQL's, and the PostgreSQL provider it was forked from replaces it with
    ///         <c>RETURNING</c>. Both are wrong here, and both fail on the first insert of a row with
    ///         an identity key. The form below is H2's own, and the reference accepts it:
    ///         <c>SELECT "Id" FROM FINAL TABLE (INSERT INTO "Items" ("Name") VALUES (?))</c>.
    ///     </para>
    /// </remarks>
    protected virtual void AppendInsertCommand(
        StringBuilder commandStringBuilder,
        string name,
        string? schema,
        IReadOnlyList<IColumnModification> writeOperations,
        IReadOnlyList<IColumnModification> readOperations,
        bool overridingSystemValue)
    {
        if (readOperations.Count == 0)
        {
            AppendInsertBody(commandStringBuilder, name, schema, writeOperations, overridingSystemValue);
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
            return;
        }

        commandStringBuilder.Append("SELECT ");
        for (var i = 0; i < readOperations.Count; i++)
        {
            if (i > 0)
            {
                commandStringBuilder.Append(", ");
            }

            commandStringBuilder.Append(SqlGenerationHelper.DelimitIdentifier(readOperations[i].ColumnName));
        }

        commandStringBuilder.Append(" FROM FINAL TABLE (");
        AppendInsertBody(commandStringBuilder, name, schema, writeOperations, overridingSystemValue);
        commandStringBuilder.Append(')');
        commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
    }

    /// <summary>
    ///     Writes the insert itself: the table, the columns, the values.
    /// </summary>
    private void AppendInsertBody(
        StringBuilder commandStringBuilder,
        string name,
        string? schema,
        IReadOnlyList<IColumnModification> writeOperations,
        bool overridingSystemValue)
    {
        AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);

        if (overridingSystemValue)
        {
            commandStringBuilder.AppendLine().Append("OVERRIDING SYSTEM VALUE");
        }

        AppendValuesHeader(commandStringBuilder, writeOperations);
        AppendValues(commandStringBuilder, name, schema, writeOperations);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override ResultSetMapping AppendUpdateOperation(
        StringBuilder commandStringBuilder,
        IReadOnlyModificationCommand command,
        int commandPosition,
        out bool requiresTransaction)
    {
        // The default implementation adds RETURNING 1 to do concurrency check (was the row actually updated), but in H2 we check
        // the per-statement row-affected value exposed by H2 in the batch; so no need for RETURNING 1.
        var name = command.TableName;
        var schema = command.Schema;
        var operations = command.ColumnModifications;

        var writeOperations = operations.Where(o => o.IsWrite).ToList();
        var conditionOperations = operations.Where(o => o.IsCondition).ToList();
        var readOperations = operations.Where(o => o.IsRead).ToList();

        requiresTransaction = false;

        AppendUpdateCommand(commandStringBuilder, name, schema, writeOperations, readOperations, conditionOperations);

        return readOperations.Count > 0 ? ResultSetMapping.LastInResultSet : ResultSetMapping.NoResults;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void AppendUpdateCommand(
        StringBuilder commandStringBuilder,
        string name,
        string? schema,
        IReadOnlyList<IColumnModification> writeOperations,
        IReadOnlyList<IColumnModification> readOperations,
        IReadOnlyList<IColumnModification> conditionOperations,
        bool appendReturningOneClause = false)
    {
        if (readOperations.Count == 0)
        {
            // The count of the affected rows is read from the batch step, so no RETURNING 1 is needed.
            AppendUpdateCommandHeader(commandStringBuilder, name, schema, writeOperations);
            AppendWhereClause(commandStringBuilder, conditionOperations);
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
            return;
        }

        // H2 2.5 has no RETURNING: the row an update leaves is read through FINAL TABLE, the same
        // form the insert uses (the grammar has { OLD | NEW | FINAL } TABLE for both).
        commandStringBuilder.Append("SELECT ");
        for (var i = 0; i < readOperations.Count; i++)
        {
            if (i > 0)
            {
                commandStringBuilder.Append(", ");
            }

            commandStringBuilder.Append(SqlGenerationHelper.DelimitIdentifier(readOperations[i].ColumnName));
        }

        commandStringBuilder.Append(" FROM FINAL TABLE (");
        AppendUpdateCommandHeader(commandStringBuilder, name, schema, writeOperations);
        AppendWhereClause(commandStringBuilder, conditionOperations);
        commandStringBuilder.Append(')');
        commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
    }

    /// <inheritdoc />
    protected override void AppendUpdateColumnValue(
        ISqlGenerationHelper updateSqlGeneratorHelper,
        IColumnModification columnModification,
        StringBuilder stringBuilder,
        string name,
        string? schema)
    {
        if (columnModification.JsonPath is not (null or "$"))
        {
            Check.DebugAssert(
                columnModification.TypeMapping is H2OwnedJsonTypeMapping,
                "ColumnModification with JsonPath but non-H2OwnedJsonTypeMapping");

            throw new NotSupportedException(
                "H2 2.5 cannot update a JSON column in part: the method jsonb_set is PostgreSQL's, and "
                + "H2 2.5 has no way to change a part of a JSON document.");
        }
        else
        {
            base.AppendUpdateColumnValue(updateSqlGeneratorHelper, columnModification, stringBuilder, name, schema);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override ResultSetMapping AppendDeleteOperation(
        StringBuilder commandStringBuilder,
        IReadOnlyModificationCommand command,
        int commandPosition,
        out bool requiresTransaction)
    {
        // The default implementation adds RETURNING 1 to do concurrency check (was the row actually deleted), but in H2 we check
        // the per-statement row-affected value exposed by H2 in the batch; so no need for RETURNING 1.
        var name = command.TableName;
        var schema = command.Schema;
        var conditionOperations = command.ColumnModifications.Where(o => o.IsCondition).ToList();

        requiresTransaction = false;

        AppendDeleteCommand(commandStringBuilder, name, schema, [], conditionOperations);

        return ResultSetMapping.NoResults;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override ResultSetMapping AppendStoredProcedureCall(
        StringBuilder commandStringBuilder,
        IReadOnlyModificationCommand command,
        int commandPosition,
        out bool requiresTransaction)
    {
        Check.DebugAssert(command.StoreStoredProcedure is not null, "command.StoreStoredProcedure is not null");

        var storedProcedure = command.StoreStoredProcedure;

        Check.DebugAssert(
            storedProcedure.Parameters.Any() || storedProcedure.ResultColumns.Any(),
            "Stored procedure call with neither parameters nor result columns");

        var resultSetMapping = ResultSetMapping.NoResults;

        commandStringBuilder.Append("CALL ");

        // H2 supports neither a return value nor a result set with stored procedures, only output parameters.
        Check.DebugAssert(storedProcedure.ReturnValue is null, "storedProcedure.Return is null");
        Check.DebugAssert(!storedProcedure.ResultColumns.Any(), "!storedProcedure.ResultColumns.Any()");

        SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, storedProcedure.Name, storedProcedure.Schema);

        commandStringBuilder.Append('(');

        var first = true;

        // Only positional parameter style supported for now, see https://github.com/dotnet/efcore/issues/28439

        // Note: the column modifications are already ordered according to the sproc parameter ordering
        // (see ModificationCommand.GenerateColumnModifications)
        for (var i = 0; i < command.ColumnModifications.Count; i++)
        {
            var columnModification = command.ColumnModifications[i];
            var parameter = (IStoreStoredProcedureParameter)columnModification.Column!;

            if (first)
            {
                first = false;
            }
            else
            {
                commandStringBuilder.Append(", ");
            }

            Check.DebugAssert(columnModification.UseParameter, "Column modification matched a parameter, but UseParameter is false");

            if (parameter.Direction == ParameterDirection.Output)
            {
                // Recommended PG practice is to pass NULL for output parameters
                commandStringBuilder.Append("NULL");
            }
            else
            {
                SqlGenerationHelper.GenerateParameterNamePlaceholder(
                    commandStringBuilder, columnModification.UseOriginalValueParameter
                        ? columnModification.OriginalParameterName!
                        : columnModification.ParameterName!);
            }

            // H2 stored procedures cannot return a regular result set, and output parameter values are simply sent back as the
            // result set; this is very different from SQL Server, where output parameter values can be sent back in addition to result
            // sets.
            if (parameter.Direction.HasFlag(ParameterDirection.Output))
            {
                // The distinction between having only a rows affected output parameter and having other non-rows affected parameters
                // is important later on (i.e. whether we need to propagate or not).
                resultSetMapping = parameter == command.RowsAffectedColumn && resultSetMapping == ResultSetMapping.NoResults
                    ? ResultSetMapping.ResultSetWithRowsAffectedOnly
                    : ResultSetMapping.LastInResultSet;
            }
        }

        commandStringBuilder.Append(')');

        commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);

        requiresTransaction = true;

        return resultSetMapping;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void AppendObtainNextSequenceValueOperation(StringBuilder commandStringBuilder, string name, string? schema)
    {
        commandStringBuilder.Append("NEXT VALUE FOR ");
        SqlGenerationHelper.DelimitIdentifier(commandStringBuilder, Check.NotNull(name, nameof(name)), schema);
    }
}
