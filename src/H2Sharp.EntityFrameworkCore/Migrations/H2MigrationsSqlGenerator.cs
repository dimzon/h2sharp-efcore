using System.Globalization;
using System.Text;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Migrations.Operations;
using H2Sharp.EntityFrameworkCore.Update.Internal;

namespace H2Sharp.EntityFrameworkCore.Migrations;

/// <summary>
///     PostgreSQL-specific implementation of <see cref="MigrationsSqlGenerator" />.
/// </summary>
/// <remarks>
///     <para>
///         The service lifetime is <see cref="ServiceLifetime.Scoped" />. This means that each <see cref="DbContext" /> instance will use
///         its own instance of this service. The implementation may depend on other services registered with any lifetime. The
///         implementation does not need to be thread-safe.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see>.
///     </para>
/// </remarks>
public class H2MigrationsSqlGenerator : MigrationsSqlGenerator
{
    private IReadOnlyList<MigrationOperation> _operations = null!;
    private readonly RelationalTypeMapping _stringTypeMapping;

    /// <summary>
    ///     The backend version to target.
    /// </summary>

    /// <summary>
    ///     Creates a new <see cref="H2MigrationsSqlGenerator" /> instance.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this service.</param>
    /// <param name="npgsqlSingletonOptions">The singleton options to use.</param>
    public H2MigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        IH2SingletonOptions npgsqlSingletonOptions)
        : base(dependencies)
    {
        _stringTypeMapping = dependencies.TypeMappingSource.GetMapping(typeof(string))
            ?? throw new InvalidOperationException("No string type mapping found");
    }

    /// <inheritdoc />
    public override IReadOnlyList<MigrationCommand> Generate(
        IReadOnlyList<MigrationOperation> operations,
        IModel? model = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        IReadOnlyList<MigrationCommand> results;
        _operations = operations;

        try
        {
            results = base.Generate(operations, model, options);

            AddSequenceBumpingForSeeding();

            return results;
        }
        finally
        {
            _operations = null!;
        }

        void AddSequenceBumpingForSeeding()
        {
            // For all tables where we had data seeding insertions, find all columns mapped to properties with identity/serial value
            // generation strategy. We'll bump the sequences for those columns.
            var seededGeneratedColumns = operations
                .OfType<InsertDataOperation>()
                .Select(o => new { o.Schema, o.Table })
                .Distinct()
                .SelectMany(
                    t => model?.GetRelationalModel().FindTable(t.Table, t.Schema)?.Columns
                            .Where(
                                c => c.PropertyMappings.Any(
                                    p => p.Property.GetValueGenerationStrategy() is
                                        H2ValueGenerationStrategy.IdentityByDefaultColumn
                                        or H2ValueGenerationStrategy.IdentityAlwaysColumn
                                        or H2ValueGenerationStrategy.SerialColumn))
                        ?? [])
                .Distinct()
                .ToArray();

            if (!seededGeneratedColumns.Any())
            {
                return;
            }

            var builder = new MigrationCommandListBuilder(Dependencies);

            foreach (var c in seededGeneratedColumns)
            {
                // Weirdly, pg_get_serial_sequence accepts a standard quoted "schema"."table" inside its first
                // parameter string literal, but the second one is a column name that shouldn't be double-quoted...

                var table = Dependencies.SqlGenerationHelper.DelimitIdentifier(c.Table.Name, c.Table.Schema);
                var column = Dependencies.SqlGenerationHelper.DelimitIdentifier(c.Name);
                var unquotedColumn = c.Name.Replace("'", "''");

                // When generating idempotent scripts, migration DDL is enclosed in anonymous DO blocks,
                // where PERFORM must be used instead of SELECT
                var selectOrPerform = options.HasFlag(MigrationsSqlGenerationOptions.Idempotent)
                    ? "PERFORM"
                    : "SELECT";

                // Set the sequence's value to the greater of:
                // 1. Maximum value currently present in the column (i.e. just seeded)
                // 2. Current value of the sequence (the max value above could be out of range of the sequence,
                //    e.g. negative values seeded)
                builder
                    .AppendLine(
                        $"""
{selectOrPerform} setval(
    pg_get_serial_sequence('{table}', '{unquotedColumn}'),
    GREATEST(
        (SELECT MAX({column}) FROM {table}) + 1,
        nextval(pg_get_serial_sequence('{table}', '{unquotedColumn}'))),
    false);
""");
            }

            builder.EndCommand();

            results = results.Concat(builder.GetCommandList()).ToArray();
        }
    }

    /// <inheritdoc />
    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        if (operation is H2CreateDatabaseOperation createDatabaseOperation)
        {
            Generate(createDatabaseOperation, model, builder);
            return;
        }

        if (operation is H2DropDatabaseOperation dropDatabaseOperation)
        {
            Generate(dropDatabaseOperation, model, builder);
            return;
        }

        base.Generate(operation, model, builder);
    }

    #region Standard migrations

    /// <inheritdoc />
    protected override void Generate(
        CreateTableOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        if (!terminate && operation.Comment is not null)
        {
            throw new ArgumentException(
                $"When generating migrations SQL for {nameof(CreateTableOperation)}, can't produce unterminated SQL with comments");
        }

        operation.Columns.RemoveAll(c => IsSystemColumn(c.Name));

        builder.Append("CREATE ");

        builder
            .Append("TABLE ")
            .Append(DelimitIdentifier(operation.Name, operation.Schema))
            .AppendLine(" (");

        using (builder.Indent())
        {
            CreateTableColumns(operation, model, builder);
            CreateTableConstraints(operation, model, builder);
            builder.AppendLine();
        }

        builder.Append(")");


        // Comment on the table
        if (operation.Comment is not null)
        {
            builder.AppendLine(";");

            builder
                .Append("COMMENT ON TABLE ")
                .Append(DelimitIdentifier(operation.Name, operation.Schema))
                .Append(" IS ")
                .Append(_stringTypeMapping.GenerateSqlLiteral(operation.Comment));
        }

        // Comments on the columns
        foreach (var columnOp in operation.Columns.Where(c => c.Comment is not null))
        {
            var columnComment = columnOp.Comment;
            builder.AppendLine(";");

            builder
                .Append("COMMENT ON COLUMN ")
                .Append(DelimitIdentifier(operation.Name, operation.Schema))
                .Append(".")
                .Append(DelimitIdentifier(columnOp.Name))
                .Append(" IS ")
                .Append(_stringTypeMapping.GenerateSqlLiteral(columnComment));
        }

        if (terminate)
        {
            builder.AppendLine(";");
            EndStatement(builder);
        }
    }

    /// <inheritdoc />
    protected override void Generate(AlterTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        var alterTableBaseSql = $"ALTER TABLE {DelimitIdentifier(operation.Name, operation.Schema)}";
        var madeChanges = false;

        // Storage parameters
        // Comment
        if (operation.Comment != operation.OldTable.Comment)
        {
            builder
                .Append("COMMENT ON TABLE ")
                .Append(DelimitIdentifier(operation.Name, operation.Schema))
                .Append(" IS ")
                .Append(_stringTypeMapping.GenerateSqlLiteral(operation.Comment));

            builder.AppendLine(";");
            madeChanges = true;
        }

        if (madeChanges)
        {
            EndStatement(builder);
        }
    }

    /// <inheritdoc />
    protected override void Generate(
        DropColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        // Never touch system columns
        if (IsSystemColumn(operation.Name))
        {
            return;
        }

        base.Generate(operation, model, builder, terminate);
    }

    /// <inheritdoc />
    protected override void Generate(
        AddColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        if (!terminate && operation.Comment is not null)
        {
            throw new ArgumentException(
                $"When generating migrations SQL for {nameof(AddColumnOperation)}, can't produce unterminated SQL with comments");
        }

        // Never touch system columns
        if (IsSystemColumn(operation.Name))
        {
            return;
        }

        if (operation[H2AnnotationNames.ValueGenerationStrategy] is H2ValueGenerationStrategy strategy)
        {
            switch (strategy)
            {
                case H2ValueGenerationStrategy.SerialColumn:
                case H2ValueGenerationStrategy.IdentityAlwaysColumn:
                case H2ValueGenerationStrategy.IdentityByDefaultColumn:
                    // NB: This gets added to all added non-nullable columns by MigrationsModelDiffer. We need to suppress
                    // it, here because PG can't have both IDENTITY/SERIAL and a DEFAULT constraint on the same column.
                    operation.DefaultValue = null;
                    break;
            }
        }

        base.Generate(operation, model, builder, terminate: false);

        if (operation.Comment is not null)
        {
            builder.AppendLine(";");

            builder
                .Append("COMMENT ON COLUMN ")
                .Append(DelimitIdentifier(operation.Table, operation.Schema))
                .Append(".")
                .Append(DelimitIdentifier(operation.Name))
                .Append(" IS ")
                .Append(_stringTypeMapping.GenerateSqlLiteral(operation.Comment));
        }

        if (terminate)
        {
            builder.AppendLine(";");
            EndStatement(builder);
        }
    }

    /// <inheritdoc />
    protected override void Generate(AlterColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        // Never touch system columns
        if (IsSystemColumn(operation.Name))
        {
            return;
        }

        var column = model?.GetRelationalModel().FindTable(operation.Table, operation.Schema)
            ?.Columns.FirstOrDefault(c => c.Name == operation.Name);


        // Note: OldColumn doesn't have Schema, Table or Name populated (https://github.com/dotnet/efcore/issues/28041), so we take these
        // from the new column (they're identical in any case).

        if (operation.ComputedColumnSql != operation.OldColumn.ComputedColumnSql
            || operation.IsStored != operation.OldColumn.IsStored)
        {
            // TODO: The following will fail if the column being altered is part of an index.
            // SqlServer recreates indexes, but wait to see if PostgreSQL will introduce a proper ALTER TABLE ALTER COLUMN
            // that allows us to do this cleanly.
            var dropColumnOperation = new DropColumnOperation
            {
                Schema = operation.Schema,
                Table = operation.Table,
                Name = operation.Name
            };

            if (column is not null)
            {
                dropColumnOperation.AddAnnotations(column.GetAnnotations());
            }

            Generate(dropColumnOperation, model, builder);

            var addColumnOperation = new AddColumnOperation
            {
                Schema = operation.Schema,
                Table = operation.Table,
                Name = operation.Name,
                ClrType = operation.ClrType,
                ColumnType = operation.ColumnType,
                IsUnicode = operation.IsUnicode,
                IsFixedLength = operation.IsFixedLength,
                MaxLength = operation.MaxLength,
                Precision = operation.Precision,
                Scale = operation.Scale,
                IsRowVersion = operation.IsRowVersion,
                IsNullable = operation.IsNullable,
                DefaultValue = operation.DefaultValue,
                DefaultValueSql = operation.DefaultValueSql,
                ComputedColumnSql = operation.ComputedColumnSql,
                IsStored = operation.IsStored,
                Comment = operation.Comment,
                Collation = operation.Collation
            };
            addColumnOperation.AddAnnotations(operation.GetAnnotations());
            Generate(addColumnOperation, model, builder);
            RecreateIndexes(column, operation, builder);
            builder.EndCommand();

            return;
        }

        string? newSequenceName = null;

        var alterBase = $"ALTER TABLE {DelimitIdentifier(operation.Table, operation.Schema)} "
            + $"ALTER COLUMN {DelimitIdentifier(operation.Name)} ";

        // TYPE + COLLATION
        var type = operation.ColumnType ?? GetColumnType(operation.Schema, operation.Table, operation.Name, operation, model)!;
        var oldType = IsOldColumnSupported(model)
            ? operation.OldColumn.ColumnType ?? GetColumnType(operation.Schema, operation.Table, operation.Name, operation.OldColumn, model)
            : null;

        // H2 2.5 has no collation of a column: the reference reads "COLLATE" only in the
        // compatibility mode, and the catalogue of H2 2.5 has no collation of a column.
        if (operation.Collation is not null)
        {
            throw new NotSupportedException(
                "H2 2.5 has no collation of a column, so the collation of the column "
                + $"'{operation.Name}' cannot be written as SQL.");
        }

        if (type != oldType)
        {
            builder
                .Append(alterBase)
                .Append("TYPE ")
                .Append(type)
                .AppendLine(";");
        }

        if (operation is { IsNullable: true, OldColumn.IsNullable: false })
        {
            builder
                .Append(alterBase)
                .Append("DROP NOT NULL")
                .AppendLine(";");
        }
        else if (operation is { IsNullable: false, OldColumn.IsNullable: true })
        {
            // The column is being made non-nullable. Generate an update statement before doing that, to convert any existing null values to
            // the default value (otherwise PostgreSQL fails).
            if (operation.DefaultValueSql is not null || operation.DefaultValue is not null)
            {
                string defaultValueSql;
                if (operation.DefaultValueSql is not null)
                {
                    defaultValueSql = operation.DefaultValueSql;
                }
                else
                {
                    Check.DebugAssert(operation.DefaultValue is not null, "operation.DefaultValue is not null");

                    var typeMapping = (type != null
                            ? Dependencies.TypeMappingSource.FindMapping(operation.DefaultValue.GetType(), type)
                            : null)
                        ?? Dependencies.TypeMappingSource.GetMappingForValue(operation.DefaultValue);

                    defaultValueSql = typeMapping.GenerateSqlLiteral(operation.DefaultValue);
                }

                builder
                    .Append("UPDATE ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema))
                    .Append(" SET ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
                    .Append(" = ")
                    .Append(defaultValueSql)
                    .Append(" WHERE ")
                    .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
                    .Append(" IS NULL")
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            }

            builder
                .Append(alterBase)
                .Append("SET NOT NULL")
                .AppendLine(";");
        }

        CheckForOldValueGenerationAnnotation(operation);

        var oldStrategy = operation.OldColumn[H2AnnotationNames.ValueGenerationStrategy] as H2ValueGenerationStrategy?;
        var newStrategy = operation[H2AnnotationNames.ValueGenerationStrategy] as H2ValueGenerationStrategy?;

        if (oldStrategy != newStrategy)
        {
            // We have a value generation strategy change

            if (oldStrategy == H2ValueGenerationStrategy.SerialColumn)
            {
                // TODO: It would be better to actually select for the owned sequence.
                // This would require plpgsql.
                var sequence = DelimitIdentifier($"{operation.Table}_{operation.Name}_seq", operation.Schema);
                switch (newStrategy)
                {
                    case null:
                        // Drop the serial, converting the column to a regular int
                        builder.AppendLine($"DROP SEQUENCE {sequence} CASCADE;");
                        break;
                    case H2ValueGenerationStrategy.IdentityAlwaysColumn:
                    case H2ValueGenerationStrategy.IdentityByDefaultColumn:
                        // Convert serial column to identity, maintaining the current sequence value
                        var identityTypeClause = newStrategy == H2ValueGenerationStrategy.IdentityAlwaysColumn
                            ? "ALWAYS"
                            : "BY DEFAULT";
                        var oldSequence = DelimitIdentifier($"{operation.Table}_{operation.Name}_old_seq", operation.Schema);
                        var oldSequenceWithoutSchema = DelimitIdentifier($"{operation.Table}_{operation.Name}_old_seq");
                        builder
                            .AppendLine($"ALTER SEQUENCE {sequence} RENAME TO {oldSequenceWithoutSchema};")
                            .AppendLine($"{alterBase}DROP DEFAULT;")
                            .AppendLine($"{alterBase}ADD GENERATED {identityTypeClause} AS IDENTITY;")
                            // When generating idempotent scripts, migration DDL is enclosed in anonymous DO blocks,
                            // where PERFORM must be used instead of SELECT
                            .Append(Options.HasFlag(MigrationsSqlGenerationOptions.Idempotent) ? "PERFORM" : "SELECT")
                            .AppendLine($" * FROM setval('{sequence}', nextval('{oldSequence}'), false);")
                            .AppendLine($"DROP SEQUENCE {oldSequence};");
                        break;
                    default:
                        throw new NotSupportedException($"Don't know how to migrate serial column to {newStrategy}");
                }
            }
            else if (oldStrategy.IsIdentity())
            {
                switch (newStrategy)
                {
                    case null:
                        // Drop the identity, converting the column to a regular int
                        builder.Append(alterBase).AppendLine("DROP IDENTITY;");
                        break;
                    case H2ValueGenerationStrategy.IdentityAlwaysColumn:
                        builder.Append(alterBase).AppendLine("SET GENERATED ALWAYS;");
                        break;
                    case H2ValueGenerationStrategy.IdentityByDefaultColumn:
                        builder.Append(alterBase).AppendLine("SET GENERATED BY DEFAULT;");
                        break;
                    case H2ValueGenerationStrategy.SerialColumn:
                        throw new NotSupportedException("Migrating from identity to serial isn't currently supported (and is a bad idea)");
                    default:
                        throw new NotSupportedException($"Don't know how to migrate identity column to {newStrategy}");
                }
            }
            else if (oldStrategy is null)
            {
                switch (newStrategy)
                {
                    case H2ValueGenerationStrategy.IdentityAlwaysColumn:
                    case H2ValueGenerationStrategy.IdentityByDefaultColumn:
                        builder.Append(alterBase).AppendLine("DROP DEFAULT;");
                        builder.Append(alterBase).Append("ADD");
                        IdentityDefinition(operation, builder);
                        builder.AppendLine(";");
                        break;
                    case H2ValueGenerationStrategy.SerialColumn:
                        switch (type)
                        {
                            case "integer":
                            case "int":
                            case "int4":
                            case "bigint":
                            case "int8":
                            case "smallint":
                            case "int2":
                                newSequenceName = $"{operation.Table}_{operation.Name}_seq";
                                Generate(
                                    new CreateSequenceOperation
                                    {
                                        Schema = operation.Schema,
                                        Name = newSequenceName,
                                        ClrType = operation.ClrType
                                    }, model, builder);

                                builder.Append(alterBase).Append("SET");
                                DefaultValue(null, $@"nextval('{DelimitIdentifier(newSequenceName, operation.Schema)}')", type, builder);
                                builder.AppendLine(";");
                                // Note: we also need to set the sequence ownership, this is done below after the ALTER COLUMN
                                break;
                        }

                        break;
                    default:
                        throw new NotSupportedException($"Don't know how to apply value generation strategy {newStrategy}");
                }
            }
        }

        // Identity sequence options may have changed
        if (oldStrategy.IsIdentity() && newStrategy.IsIdentity())
        {
            var newSequenceOptions = IdentitySequenceOptionsData.Get(operation);
            var oldSequenceOptions = IdentitySequenceOptionsData.Get(operation.OldColumn);

            if (newSequenceOptions.StartValue != oldSequenceOptions.StartValue)
            {
                var startValue = newSequenceOptions.StartValue ?? 1;

                builder
                    .Append(alterBase)
                    .Append("RESTART WITH ")
                    .Append(startValue.ToString(CultureInfo.InvariantCulture))
                    .AppendLine(";");
            }

            if (newSequenceOptions.IncrementBy != oldSequenceOptions.IncrementBy)
            {
                builder
                    .Append(alterBase)
                    .Append("SET INCREMENT BY ")
                    .Append(newSequenceOptions.IncrementBy.ToString(CultureInfo.InvariantCulture))
                    .AppendLine(";");
            }

            if (newSequenceOptions.MinValue != oldSequenceOptions.MinValue)
            {
                builder
                    .Append(alterBase)
                    .Append(
                        newSequenceOptions.MinValue is null
                            ? "SET NO MINVALUE"
                            : "SET MINVALUE " + newSequenceOptions.MinValue)
                    .AppendLine(";");
            }

            if (newSequenceOptions.MaxValue != oldSequenceOptions.MaxValue)
            {
                builder
                    .Append(alterBase)
                    .Append(
                        newSequenceOptions.MaxValue is null
                            ? "SET NO MAXVALUE"
                            : "SET MAXVALUE " + newSequenceOptions.MaxValue)
                    .AppendLine(";");
            }

            if (newSequenceOptions.IsCyclic != oldSequenceOptions.IsCyclic)
            {
                builder
                    .Append(alterBase)
                    .Append(
                        newSequenceOptions.IsCyclic
                            ? "SET CYCLE"
                            : "SET NO CYCLE")
                    .AppendLine(";");
            }

            if (newSequenceOptions.NumbersToCache != oldSequenceOptions.NumbersToCache)
            {
                builder
                    .Append(alterBase)
                    .Append("SET CACHE ")
                    .Append(newSequenceOptions.NumbersToCache.ToString(CultureInfo.InvariantCulture))
                    .AppendLine(";");
            }
        }

        // DEFAULT.
        // Note that defaults values for value-generated columns (identity, serial) are managed above. This is
        // only for regular columns with user-specified default settings.
        if (newStrategy is null
            && (operation.DefaultValueSql != operation.OldColumn.DefaultValueSql
                || !Equals(operation.DefaultValue, operation.OldColumn.DefaultValue)))
        {
            builder.Append(alterBase);
            if (operation.DefaultValue is not null || operation.DefaultValueSql is not null)
            {
                builder.Append("SET");
                DefaultValue(operation.DefaultValue, operation.DefaultValueSql, type, builder);
            }
            else
            {
                builder.Append("DROP DEFAULT");
            }

            builder.AppendLine(";");
        }

        // A sequence has been created because this column was altered to be a serial.
        // Change the sequence's ownership.
        if (newSequenceName is not null)
        {
            builder
                .Append("ALTER SEQUENCE ")
                .Append(DelimitIdentifier(newSequenceName, operation.Schema))
                .Append(" OWNED BY ")
                .Append(DelimitIdentifier(operation.Table, operation.Schema))
                .Append(".")
                .Append(DelimitIdentifier(operation.Name))
                .AppendLine(";");
        }

        // Comment
        if (operation.Comment != operation.OldColumn.Comment)
        {
            builder
                .Append("COMMENT ON COLUMN ")
                .Append(DelimitIdentifier(operation.Table, operation.Schema))
                .Append(".")
                .Append(DelimitIdentifier(operation.Name))
                .Append(" IS ")
                .Append(_stringTypeMapping.GenerateSqlLiteral(operation.Comment))
                .AppendLine(";");
        }

        EndStatement(builder);
    }

    /// <inheritdoc />
    protected override void Generate(RenameIndexOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        if (operation.NewName is not null && operation.NewName != operation.Name)
        {
            Rename(operation.Schema, operation.Name, operation.NewName, "INDEX", builder);
        }

        // N.B. indexes are always stored in the same schema as the table.
        EndStatement(builder);
    }

    /// <inheritdoc />
    protected override void Generate(RenameSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        var name = operation.Name;
        if (operation.NewName is not null && operation.NewName != operation.Name)
        {
            Rename(operation.Schema, operation.Name, operation.NewName, "SEQUENCE", builder);

            name = operation.NewName;
        }

        if (operation.NewSchema is not null && operation.NewSchema != operation.Schema)
        {
            Transfer(operation.NewSchema, operation.Schema, name, "SEQUENCE", builder);
        }

        EndStatement(builder);
    }

    /// <inheritdoc />
    protected override void SequenceOptions(
        string? schema,
        string name,
        SequenceOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool forAlter)
    {
        var intTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(int));
        var longTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(long));

        builder
            .Append(" INCREMENT BY ")
            .Append(intTypeMapping.GenerateSqlLiteral(operation.IncrementBy));

        if (operation.MinValue != null)
        {
            builder
                .Append(" MINVALUE ")
                .Append(longTypeMapping.GenerateSqlLiteral(operation.MinValue));
        }
        else if (forAlter)
        {
            builder
                .Append(" NO MINVALUE");
        }

        if (operation.MaxValue != null)
        {
            builder
                .Append(" MAXVALUE ")
                .Append(longTypeMapping.GenerateSqlLiteral(operation.MaxValue));
        }
        else if (forAlter)
        {
            builder
                .Append(" NO MAXVALUE");
        }

        builder.Append(operation.IsCyclic ? " CYCLE" : " NO CYCLE");
    }

    /// <inheritdoc />
    protected override void Generate(RestartSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // PostgreSQL has ALTER SEQUENCE ... RESTART WITH x, which resets the current sequence value but does not change its start value
        // in the schema (so a subsequence RESTART without an argument resets it back to its original start value, not to x).
        // It also has ALTER SEQUENCE ... STARTS WITH x, which resets the schema start value but not the current value.
        // So we use both statements to reset both the current value and the schema value.
        if (operation.StartValue.HasValue)
        {
            var longTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(long));

            builder
                .Append("ALTER SEQUENCE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(" START WITH ")
                .Append(longTypeMapping.GenerateSqlLiteral(operation.StartValue.Value))
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);

            builder
                .Append("ALTER SEQUENCE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(" RESTART")
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
        }
        else
        {
            builder
                .Append("ALTER SEQUENCE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(" RESTART")
                .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
        }

        EndStatement(builder);
    }

    /// <inheritdoc />
    protected override void Generate(RenameTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        var name = operation.Name;
        if (operation.NewName is not null && operation.NewName != operation.Name)
        {
            Rename(operation.Schema, operation.Name, operation.NewName, "TABLE", builder);

            name = operation.NewName;
        }

        if (operation.NewSchema is not null && operation.NewSchema != operation.Schema)
        {
            Transfer(operation.NewSchema, operation.Schema, name, "TABLE", builder);
        }

        EndStatement(builder);
    }

    /// <inheritdoc />
    protected override void Generate(
        CreateIndexOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        // H2 2.5 knows the default index and the spatial index. Its hash index is read only in the
        // compatibility mode, and the index methods of PostgreSQL are not in H2 2.5 at all.
        var method = operation[H2AnnotationNames.IndexMethod] as string;
        if (method is not null && method is not "btree" and not "spatial")
        {
            throw new NotSupportedException(
                $"H2 2.5 has no index of the kind '{method}': the kinds of the index of H2 2.5 are the "
                + "default index (btree) and the spatial index.");
        }

        builder.Append("CREATE ");

        if (operation.IsUnique)
        {
            builder.Append("UNIQUE ");
        }

        if (method is "spatial")
        {
            builder.Append("SPATIAL ");
        }

        builder.Append("INDEX ");

        builder
            .Append(DelimitIdentifier(operation.Name))
            .Append(" ON ")
            .Append(DelimitIdentifier(operation.Table, operation.Schema));

        var indexColumns = GetIndexColumns(operation);

        builder
            .Append(" (")
            .Append(IndexColumnList(indexColumns, method))
            .Append(")");

        IndexOptions(operation, model, builder);

        if (terminate)
        {
            builder.AppendLine(";");
            // Concurrent indexes cannot be created within a transaction
            EndStatement(builder);
        }
    }

    /// <inheritdoc />
    protected override void IndexOptions(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        // H2 2.5 has no partial index: the reference reads the columns of the index and nothing
        // else, and a WHERE clause after them is a syntax error.
        if (operation is CreateIndexOperation { Filter: not null })
        {
            throw new NotSupportedException(
                "H2 2.5 has no partial index, so the filter of an index cannot be written as SQL.");
        }

        if (operation[H2AnnotationNames.IndexInclude] is string[] { Length: > 0 } includeColumns)
        {
            // H2 2.5 reads the clause INCLUDE only for a unique index: the reference reads it in the
            // branch of the unique index, and a non-unique index with INCLUDE is a syntax error.
            if (operation is not CreateIndexOperation { IsUnique: true })
            {
                throw new NotSupportedException(
                    "H2 2.5 reads the clause INCLUDE only for a unique index, so the included columns "
                    + "of a non-unique index cannot be written as SQL.");
            }

            builder
                .Append(" INCLUDE (")
                .Append(ColumnList(includeColumns))
                .Append(")");
        }

        if (operation[H2AnnotationNames.NullsDistinct] is false)
        {
            builder.Append(" NULLS NOT DISTINCT");
        }


        base.IndexOptions(operation, model, builder);
    }

    /// <inheritdoc />
    protected override void Generate(EnsureSchemaOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        if (operation.Name == "public")
        {
            return;
        }

        // H2 2.5 has CREATE SCHEMA IF NOT EXISTS, and the reference takes it -- twice over,
        // the second time as a no-op. PostgreSQL has the clause too, but it asks for CREATE
        // privileges on the database, and the provider wrapped the DDL in a DO block to get
        // around that. H2 has no DO block at all, and its catalogue is INFORMATION_SCHEMA,
        // not pg_namespace.
        builder
            .Append("CREATE SCHEMA IF NOT EXISTS ")
            .Append(DelimitIdentifier(operation.Name))
            .AppendLine(";");

        EndStatement(builder);
    }

    /// <inheritdoc />
    protected virtual void Generate(H2CreateDatabaseOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        builder
            .Append("CREATE DATABASE ")
            .Append(DelimitIdentifier(operation.Name));

        if (!string.IsNullOrEmpty(operation.Template))
        {
            builder
                .AppendLine()
                .Append("TEMPLATE ")
                .Append(DelimitIdentifier(operation.Template));
        }

        if (!string.IsNullOrEmpty(operation.Collation))
        {
            throw new NotSupportedException(
                "H2 2.5 has no LC_COLLATE of a database: the collation of the database is set "
                + "with SET COLLATION, and only with the names H2 2.5 has.");
        }

        builder.AppendLine(";");

        EndStatement(builder, suppressTransaction: true);
    }

    /// <inheritdoc />
    public virtual void Generate(H2DropDatabaseOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        var dbName = DelimitIdentifier(operation.Name);

        if (true)
        {
            builder.AppendLine($"DROP DATABASE {dbName} WITH (FORCE);");
        }
        EndStatement(builder, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Generate(
        AlterDatabaseOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        if (operation.Collation != operation.OldDatabase.Collation)
        {
            throw new NotSupportedException(
                "H2 2.5 does not support altering the collation of an existing database.");
        }

        GenerateEnumStatements(operation, model, builder);

        builder.EndCommand();
    }

    #region Enum management

    /// <inheritdoc />
    protected virtual void GenerateEnumStatements(AlterDatabaseOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        foreach (var enumTypeToCreate in operation.GetH2Enums()
                     .Where(ne => operation.GetOldH2Enums().All(oe => oe.Name != ne.Name || oe.Schema != ne.Schema)))
        {
            GenerateCreateEnum(enumTypeToCreate, model, builder);
        }

        foreach (var enumTypeToDrop in operation.GetOldH2Enums()
                     .Where(oe => operation.GetH2Enums().All(ne => ne.Name != oe.Name || oe.Schema != ne.Schema)))
        {
            GenerateDropEnum(enumTypeToDrop, model, builder);
        }

        foreach (var (newEnum, oldEnum) in operation.GetH2Enums().OrderBy(e => e.Schema).ThenBy(e => e.Name)
                     .Join(
                         operation.GetOldH2Enums().OrderBy(e => e.Schema).ThenBy(e => e.Name),
                         e => new { e.Name, e.Schema },
                         e => new { e.Name, e.Schema },
                         (ne, oe) => (New: ne, Old: oe)))
        {
            var (oldLabels, newLabels) = (oldEnum.Labels, newEnum.Labels);

            // H2 2.5 has no way of adding a label to an enum: ALTER DOMAIN takes only a
            // constraint or a check, and the reference refuses "ALTER DOMAIN m ADD VALUE 'c'".
            // The type of H2 is written whole, and a type whose labels changed cannot be
            // written that way.
            if (oldLabels.Except(newLabels).FirstOrDefault() is { } removedLabel)
            {
                throw new NotSupportedException(
                    $"Can't remove enum label '{removedLabel}' from enum type '{newEnum}'. "
                    + "H2 2.5 has no way of changing the labels of an enum.");
            }

            if (newLabels.Except(oldLabels).FirstOrDefault() is { } addedLabel)
            {
                throw new NotSupportedException(
                    $"Can't add enum label '{addedLabel}' to enum type '{newEnum}'. "
                    + "H2 2.5 has no way of changing the labels of an enum.");
            }
        }
    }

    /// <inheritdoc />
    protected virtual void GenerateCreateEnum(H2Enum enumType, IModel? model, MigrationCommandListBuilder builder)
    {
        var schema = enumType.Schema ?? model?.GetDefaultSchema();

        // Schemas are normally created (or rather ensured) by the model differ, which scans all tables, sequences
        // and other database objects. However, it isn't aware of enums, so we always ensure schema on enum creation.
        if (schema is not null)
        {
            Generate(new EnsureSchemaOperation { Name = schema }, model, builder);
        }

        builder
            .Append("CREATE TYPE ")
            .Append(DelimitIdentifier(enumType.Name, schema))
            .Append(" AS ENUM (");

        var labels = enumType.Labels;
        for (var i = 0; i < labels.Count; i++)
        {
            builder.Append(_stringTypeMapping.GenerateSqlLiteral(labels[i]));
            if (i < labels.Count - 1)
            {
                builder.Append(", ");
            }
        }

        builder.AppendLine(");");
    }

    /// <inheritdoc />
    protected virtual void GenerateDropEnum(H2Enum enumType, IModel? model, MigrationCommandListBuilder builder)
    {
        var schema = enumType.Schema ?? model?.GetDefaultSchema();

        builder
            .Append("DROP TYPE ")
            .Append(DelimitIdentifier(enumType.Name, schema))
            .AppendLine(";");
    }

    #endregion Enum management

    // Range management: PostgreSQL has user-defined range types and EF can create and drop them as part
    // of a migration. H2 has no range types at all, so there is nothing to generate here.

    /// <inheritdoc />
    protected override void Generate(
        DropIndexOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        builder
            .Append("DROP INDEX ")
            .Append(DelimitIdentifier(operation.Name, operation.Schema));

        if (terminate)
        {
            builder.AppendLine(";");
            EndStatement(builder);
        }
    }

    /// <inheritdoc />
    protected override void Generate(RenameColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        builder.Append("ALTER TABLE ")
            .Append(DelimitIdentifier(operation.Table, operation.Schema))
            .Append(" RENAME COLUMN ")
            .Append(DelimitIdentifier(operation.Name))
            .Append(" TO ")
            .Append(DelimitIdentifier(operation.NewName))
            .AppendLine(";");

        EndStatement(builder);
    }

    /// <summary>
    ///     Builds commands for the given <see cref="InsertDataOperation" /> by making calls on the given
    ///     <see cref="MigrationCommandListBuilder" />, and then terminates the final command.
    /// </summary>
    /// <param name="operation"> The operation. </param>
    /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
    /// <param name="builder"> The command builder to use to build the commands. </param>
    /// <param name="terminate"> Indicates whether or not to terminate the command after generating SQL for the operation. </param>
    protected override void Generate(
        InsertDataOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        var sqlBuilder = new StringBuilder();
        foreach (var modificationCommand in GenerateModificationCommands(operation, model))
        {
            var overridingSystemValue = modificationCommand.ColumnModifications.Any(
                m =>
                    m.Property?.GetValueGenerationStrategy() == H2ValueGenerationStrategy.IdentityAlwaysColumn);
            ((H2UpdateSqlGenerator)Dependencies.UpdateSqlGenerator).AppendInsertOperation(
                sqlBuilder,
                modificationCommand,
                0,
                overridingSystemValue,
                out _);
        }

        builder.Append(sqlBuilder.ToString());

        if (terminate)
        {
            builder.EndCommand();
        }
    }

    /// <inheritdoc />
    protected override void Generate(CreateSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        Check.NotNull(operation, nameof(operation));

        // Upstream this fell back to generating the sequence without an AS clause on PostgreSQL 9.x,
        // because "CREATE SEQUENCE name AS type" is a 10-and-later form. H2's CREATE SEQUENCE has no
        // AS type clause at all, so the base generator's output is what applies here unchanged.
        base.Generate(operation, model, builder);
    }

    #endregion Standard migrations

    #region Utilities

    /// <inheritdoc />
    protected override void ColumnDefinition(
        string? schema,
        string table,
        string name,
        ColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        Check.NotEmpty(name, nameof(name));
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        if (operation.ColumnType is null)
        {
            operation.ColumnType = GetColumnType(schema, table, name, operation, model);
        }

        CheckForOldValueGenerationAnnotation(operation);
        var valueGenerationStrategy = operation[H2AnnotationNames.ValueGenerationStrategy] as H2ValueGenerationStrategy?;
        if (valueGenerationStrategy == H2ValueGenerationStrategy.SerialColumn)
        {
            if (operation.IsNullable)
            {
                throw new NotSupportedException("Identity columns can't be nullable");
            }

            // H2 has no serial, bigserial or smallserial. An auto-generated integer column is declared as
            // GENERATED { ALWAYS | BY DEFAULT } AS IDENTITY, and the integer type is spelled the H2 way.
            switch (operation.ColumnType)
            {
                case "int":
                case "int4":
                case "integer":
                    operation.ColumnType = "integer";
                    break;
                case "bigint":
                case "int8":
                    operation.ColumnType = "bigint";
                    break;
                case "smallint":
                case "int2":
                    operation.ColumnType = "smallint";
                    break;
            }
        }


        if (operation.ComputedColumnSql is not null)
        {
            ComputedColumnDefinition(schema, table, name, operation, model, builder);

            return;
        }

        var columnType = operation.ColumnType ?? GetColumnType(schema, table, name, operation, model)!;
        builder
            .Append(DelimitIdentifier(name))
            .Append(" ")
            .Append(columnType);

        // H2 2.5 has no collation of a column: the reference reads "COLLATE" only in the
        // compatibility mode, and the catalogue of H2 2.5 has no collation of a column.
        if (operation.Collation is not null)
        {
            throw new NotSupportedException(
                "H2 2.5 has no collation of a column, so the collation of the column "
                + $"'{operation.Name}' cannot be written as SQL.");
        }

        if (valueGenerationStrategy.IsIdentity())
        {
            IdentityDefinition(operation, builder);
        }
        else
        {
            if (!operation.IsNullable)
            {
                builder.Append(" NOT NULL");
            }

            DefaultValue(operation.DefaultValue, operation.DefaultValueSql, columnType, builder);
        }
    }

    /// <inheritdoc />
    protected override void DefaultValue(
        object? defaultValue,
        string? defaultValueSql,
        string? columnType,
        MigrationCommandListBuilder builder)
    {
        // This is a hacky workaround for https://github.com/dotnet/efcore/issues/32353 - the EF MigrationsModelDiffer generates an empty
        // string as the default value for JSON columns, but that's not valid as a JSON document and rejected by PG's jsonb type. So we
        // replace the empty string with an empty JSON document {}.
        // Note that even after the EF-side issue is fixed, removing this hack is a breaking change as migrations have already been
        // scaffolded with an empty string.
        if (columnType is "json" && defaultValue is "")
        {
            defaultValue = "{}";
        }

        base.DefaultValue(defaultValue, defaultValueSql, columnType, builder);
    }

    // Note: this definition is only used for creating new identity columns, not for alterations.
    /// <inheritdoc />
    protected virtual void IdentityDefinition(
        ColumnOperation operation,
        MigrationCommandListBuilder builder)
    {
        if (operation[H2AnnotationNames.ValueGenerationStrategy] is not H2ValueGenerationStrategy strategy
            || !strategy.IsIdentity())
        {
            return;
        }

        builder.Append(
            strategy == H2ValueGenerationStrategy.IdentityAlwaysColumn
                ? " GENERATED ALWAYS AS IDENTITY"
                : " GENERATED BY DEFAULT AS IDENTITY");

        // Handle sequence options for the identity column
        if (operation[H2AnnotationNames.IdentityOptions] is string identitySequenceOptions)
        {
            // TODO: Potential for refactoring with regular sequences (i.e. calling SequenceOptions),
            // but some complexity to be worked out around creating/altering/restarting

            var sequenceData = IdentitySequenceOptionsData.Deserialize(identitySequenceOptions);

            var optionsWritten = false;

            var incrementBy = sequenceData.IncrementBy;

            var defaultMinValue = incrementBy > 0 ? 1 : Min(operation.ClrType);
            var defaultMaxValue = incrementBy > 0 ? Max(operation.ClrType) : -1;

            var minValue = sequenceData.MinValue ?? defaultMinValue;
            var maxValue = sequenceData.MaxValue ?? defaultMaxValue;

            var defaultStartValue = incrementBy > 0 ? minValue : maxValue;
            if (sequenceData.StartValue.HasValue && sequenceData.StartValue != defaultStartValue)
            {
                Append("START WITH " + sequenceData.StartValue);
            }

            if (incrementBy != 1)
            {
                Append("INCREMENT BY " + incrementBy);
            }

            if (minValue != defaultMinValue)
            {
                Append("MINVALUE " + minValue);
            }

            if (maxValue != defaultMaxValue)
            {
                Append("MAXVALUE " + maxValue);
            }

            if (sequenceData.IsCyclic)
            {
                Append("CYCLE");
            }

            if (sequenceData.NumbersToCache != 1)
            {
                Append("CACHE " + sequenceData.NumbersToCache);
            }

            if (optionsWritten)
            {
                builder.Append(")");
            }

            void Append(string s)
            {
                builder
                    .Append(optionsWritten ? " " : " (")
                    .Append(s);
                optionsWritten = true;
            }

            // Note: in older versions of PostgreSQL there's a slight variation, see H2DatabaseModelFactory.
            // This is currently only used by identity, which is only supported on PG 10 anyway.
            long Min(Type type)
            {
                if (type == typeof(int))
                {
                    return int.MinValue;
                }

                if (type == typeof(long))
                {
                    return long.MinValue;
                }

                if (type == typeof(short))
                {
                    return short.MinValue;
                }

                throw new ArgumentOutOfRangeException();
            }

            long Max(Type type)
            {
                if (type == typeof(int))
                {
                    return int.MaxValue;
                }

                if (type == typeof(long))
                {
                    return long.MaxValue;
                }

                if (type == typeof(short))
                {
                    return short.MaxValue;
                }

                throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    ///     Generates a SQL fragment for a computed column definition for the given column metadata.
    /// </summary>
    /// <param name="schema"> The schema that contains the table, or <c>null</c> to use the default schema. </param>
    /// <param name="table"> The table that contains the column. </param>
    /// <param name="name"> The column name. </param>
    /// <param name="operation"> The column metadata. </param>
    /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
    /// <param name="builder"> The command builder to use to add the SQL fragment. </param>
    protected override void ComputedColumnDefinition(
        string? schema,
        string table,
        string name,
        ColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        Check.NotEmpty(name, nameof(name));
        Check.NotNull(operation, nameof(operation));
        Check.NotNull(builder, nameof(builder));

        if (false)
        {
            throw new NotSupportedException("Computed/generated columns aren't supported in PostgreSQL prior to version 12");
        }

        if (operation.IsStored != true)
        {
            throw new NotSupportedException(
                "Generated columns currently must be stored, specify 'stored: true' in "
                + $"'{nameof(RelationalPropertyBuilderExtensions.HasComputedColumnSql)}' in your context's OnModelCreating.");
        }

        builder
            .Append(DelimitIdentifier(name))
            .Append(" ")
            .Append(operation.ColumnType ?? GetColumnType(schema, table, name, operation, model)!);

        // H2 2.5 has no collation of a column: the reference reads "COLLATE" only in the
        // compatibility mode, and the catalogue of H2 2.5 has no collation of a column.
        if (operation.Collation is not null)
        {
            throw new NotSupportedException(
                "H2 2.5 has no collation of a column, so the collation of the column "
                + $"'{name}' cannot be written as SQL.");
        }

        builder
            .Append(" GENERATED ALWAYS AS (")
            .Append(operation.ComputedColumnSql!)
            .Append(") STORED");

        if (!operation.IsNullable)
        {
            builder.Append(" NOT NULL");
        }
    }

#pragma warning disable 618
    // Version 1.0 had a bad strategy for expressing serial columns, which depended on a
    // ValueGeneratedOnAdd annotation. Detect that and throw.
    private static void CheckForOldValueGenerationAnnotation(IAnnotatable annotatable)
    {
        if (annotatable.FindAnnotation(H2AnnotationNames.ValueGeneratedOnAdd) is not null)
        {
            throw new NotSupportedException(
                "The H2:ValueGeneratedOnAdd annotation has been found in your migrations, but is no longer supported. Please replace it with '.Annotation(\"H2:ValueGenerationStrategy\", H2ValueGenerationStrategy.SerialColumn)' where you want PostgreSQL serial (autoincrement) columns, and remove it in all other cases.");
        }
    }
#pragma warning restore 618

    /// <summary>
    ///     Renames a database object such as a table, index, or sequence.
    /// </summary>
    /// <param name="schema">The current schema of the object to rename.</param>
    /// <param name="name">The current name of the object to rename.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="type">The type of the object (e.g. TABLE, INDEX, SEQUENCE).</param>
    /// <param name="builder">The builder to which operations are appended.</param>
    public virtual void Rename(
        string? schema,
        string name,
        string newName,
        string type,
        MigrationCommandListBuilder builder)
    {
        Check.NotEmpty(name, nameof(name));
        Check.NotEmpty(newName, nameof(newName));
        Check.NotEmpty(type, nameof(type));
        Check.NotNull(builder, nameof(builder));

        builder
            .Append("ALTER ")
            .Append(type)
            .Append(" ")
            .Append(DelimitIdentifier(name, schema))
            .Append(" RENAME TO ")
            .Append(DelimitIdentifier(newName))
            .AppendLine(";");
    }

    /// <summary>
    ///     Transfers a database object such as a table, index, or sequence between schemas.
    /// </summary>
    /// <param name="newSchema">The new schema.</param>
    /// <param name="schema">The current schema.</param>
    /// <param name="name">The name of the object to transfer.</param>
    /// <param name="type">The type of the object (e.g. TABLE, INDEX, SEQUENCE).</param>
    /// <param name="builder">The builder to which operations are appended.</param>
    public virtual void Transfer(
        string newSchema,
        string? schema,
        string name,
        string type,
        MigrationCommandListBuilder builder)
    {
        Check.NotEmpty(newSchema, nameof(newSchema));
        Check.NotEmpty(name, nameof(name));
        Check.NotNull(type, nameof(type));
        Check.NotNull(builder, nameof(builder));

        builder
            .Append("ALTER ")
            .Append(type)
            .Append(" ")
            .Append(DelimitIdentifier(name, schema))
            .Append(" SET SCHEMA ")
            .Append(DelimitIdentifier(newSchema))
            .AppendLine(";");
    }

    /// <inheritdoc />
    protected virtual void RecreateIndexes(IColumn? column, MigrationOperation currentOperation, MigrationCommandListBuilder builder)
    {
        foreach (var index in GetIndexesToRebuild())
        {
            Generate(CreateIndexOperation.CreateFrom(index), index.Table.Model.Model, builder, terminate: false);
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
        }

        IEnumerable<ITableIndex> GetIndexesToRebuild()
        {
            if (column == null)
            {
                yield break;
            }

            var table = column.Table;
            var createIndexOperations = _operations.SkipWhile(o => o != currentOperation).Skip(1)
                .OfType<CreateIndexOperation>().Where(o => o.Table == table.Name && o.Schema == table.Schema).ToList();
            foreach (var index in table.Indexes)
            {
                var indexName = index.Name;
                if (createIndexOperations.Any(o => o.Name == indexName))
                {
                    continue;
                }

                if (index.Columns.Any(c => c == column))
                {
                    yield return index;
                }
                else if (index[H2AnnotationNames.IndexInclude] is IReadOnlyList<string> includeColumns
                         && includeColumns.Contains(column.Name))
                {
                    yield return index;
                }
            }
        }
    }

    #endregion Utilities

    #region System column utilities

    private bool IsSystemColumn(string name)
        => SystemColumnNames.Contains(name);

    /// <summary>
    ///     Tables in PostgreSQL implicitly have a set of system columns, which are always there.
    ///     We want to allow users to access these columns (i.e. xmin for optimistic concurrency) but
    ///     they should never generate migration operations.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/ddl-system-columns.html
    /// </remarks>
    private static readonly string[] SystemColumnNames = ["tableoid", "xmin", "cmin", "xmax", "cmax", "ctid"];

    #endregion System column utilities

    #region Helpers

    private string DelimitIdentifier(string identifier)
        => Dependencies.SqlGenerationHelper.DelimitIdentifier(identifier);

    private string DelimitIdentifier(string name, string? schema)
        => Dependencies.SqlGenerationHelper.DelimitIdentifier(name, schema);

    private string IndexColumnList(IndexColumn[] columns, string? method)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < columns.Length; i++)
        {
            var column = columns[i];

            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(DelimitIdentifier(column.Name));

            // Of the built-in access methods, only btree (the default) supports
            // sorting, thus we only want to emit sort options for btree indexes.
            if (method is null or "btree")
            {
                if (column.IsDescending)
                {
                    builder.Append(" DESC");
                }

                if (column.NullSortOrder != NullSortOrder.Unspecified)
                {
                    builder.Append(" NULLS ");

                    switch (column.NullSortOrder)
                    {
                        case NullSortOrder.NullsFirst:
                            builder.Append("FIRST");
                            break;
                        case NullSortOrder.NullsLast:
                            builder.Append("LAST");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        return builder.ToString();
    }

    private static bool TryParseSchema(string identifier, out string name, out string? schema)
    {
        var index = identifier.IndexOf('.');

        if (index >= 0)
        {
            schema = identifier.Substring(0, index);
            name = identifier.Substring(index + 1);
            return true;
        }

        name = identifier;
        schema = default;
        return false;
    }

    private static IndexColumn[] GetIndexColumns(CreateIndexOperation operation)
    {
        // We used to have our own annotation-based descending index mechanism, this got replaced with IsDescending in EF Core 7.0.
        var isDescendingValues = operation.IsDescending;
        var legacySortOrders = operation[H2AnnotationNames.IndexSortOrder] as SortOrder[];

        var nullSortOrders = operation[H2AnnotationNames.IndexNullSortOrder] as NullSortOrder[];

        var columns = new IndexColumn[operation.Columns.Length];

        for (var i = 0; i < columns.Length; i++)
        {
            var name = operation.Columns[i];
            var isColumnDescending = isDescendingValues is not null
                ? isDescendingValues.Length == 0 || isDescendingValues[i]
                : i < legacySortOrders?.Length && legacySortOrders[i] == SortOrder.Descending;
            var nullSortOrder = i < nullSortOrders?.Length ? nullSortOrders[i] : NullSortOrder.Unspecified;

            columns[i] = new IndexColumn(name, isColumnDescending, nullSortOrder);
        }

        return columns;
    }

    private readonly struct IndexColumn(string name, bool isDescending, NullSortOrder nullSortOrder)
    {
        public string Name { get; } = name;
        public bool IsDescending { get; } = isDescending;
        public NullSortOrder NullSortOrder { get; } = nullSortOrder;
    }


    #endregion
}
