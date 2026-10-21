using System.Data;
using System.Data.Common;
using System.Globalization;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore.Scaffolding.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EntityFramework Core release.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this class reverse-engineered a PostgreSQL database by querying pg_catalog: pg_class,
///         pg_attribute, pg_constraint, pg_index, pg_sequence, plus a catalogue_version switch on almost
///         every query. None of that exists in H2. Everything here reads INFORMATION_SCHEMA, which is the
///         standard catalogue the reference engine exposes. The column lists below were taken from the
///         reference engine rather than assumed; see <c>docs/DIALECT.md</c> for the probes.
///     </para>
///     <para>
///         Three properties of H2's catalogue shape are load-bearing and worth stating: index columns
///         live in INFORMATION_SCHEMA.INDEX_COLUMNS rather than on INDEXES, which carries no
///         COLUMN_NAME; an unquoted identifier is folded to upper case, so every name read back is upper
///         case unless the original was quoted; and the type as written in the DDL is
///         DECLARED_DATA_TYPE, while DATA_TYPE is the base name, so precision and length are read from
///         the former rather than from separate columns.
///     </para>
/// </remarks>
public class H2DatabaseModelFactory : DatabaseModelFactory
{
    private readonly IDiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2DatabaseModelFactory(IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger)
        => _logger = Check.NotNull(logger, nameof(logger));

    /// <inheritdoc />
    public override DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
    {
        Check.NotEmpty(connectionString, nameof(connectionString));
        Check.NotNull(options, nameof(options));

        using var connection = new H2Sharp.Data.H2Connection(H2RelationalConnection.H2Url(connectionString));
        return Create(connection, options);
    }

    /// <inheritdoc />
    public override DatabaseModel Create(DbConnection dbConnection, DatabaseModelFactoryOptions options)
    {
        Check.NotNull(dbConnection, nameof(dbConnection));
        Check.NotNull(options, nameof(options));

        var startedOpen = dbConnection.State == ConnectionState.Open;
        if (!startedOpen)
        {
            dbConnection.Open();
        }

        try
        {
            // The catalogue is the only way to see what a database contains. If it is missing, say so:
            // an empty model here would be indistinguishable from an empty database, and every migration
            // generated from it would try to recreate tables that already exist.
            if (!CatalogueIsAvailable(dbConnection))
            {
                throw new InvalidOperationException(
                    "Scaffolding reads INFORMATION_SCHEMA, and this H2 database does not expose it. The " +
                    "reference engine (H2 2.5) provides it; the port does not implement it yet.");
            }

            var databaseModel = new DatabaseModel
            {
                DatabaseName = dbConnection.Database,
                DefaultSchema = "PUBLIC"
            };

            var schemas = options.Schemas.Any() ? options.Schemas.ToList() : new List<string> { "PUBLIC" };
            var tables = options.Tables.Select(Parse).ToList();

            PopulateTables(dbConnection, databaseModel, schemas, tables);
            PopulateKeys(dbConnection, databaseModel, schemas);
            PopulateIndexes(dbConnection, databaseModel, schemas);
            PopulateSequences(dbConnection, databaseModel, schemas);
            PopulateForeignKeys(dbConnection, databaseModel);

            return databaseModel;
        }
        finally
        {
            if (!startedOpen)
            {
                dbConnection.Close();
            }
        }
    }

    private static bool CatalogueIsAvailable(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'INFORMATION_SCHEMA'";

        try
        {
            using var reader = command.ExecuteReader();
            return reader.Read();
        }
        catch (DbException)
        {
            return false;
        }
    }

    private static void PopulateTables(
        DbConnection connection,
        DatabaseModel databaseModel,
        IReadOnlyList<string> schemas,
        IReadOnlyList<(string? Schema, string Name)> wanted)
    {
        foreach (var schema in schemas)
        {
            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT TABLE_NAME,
                       ORDINAL_POSITION,
                       COLUMN_NAME,
                       DATA_TYPE,
                       IS_NULLABLE,
                       IS_IDENTITY,
                       IDENTITY_GENERATION,
                       IS_GENERATED,
                       GENERATION_EXPRESSION,
                       COLUMN_DEFAULT,
                       DECLARED_DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @p_schema
                ORDER BY TABLE_NAME, ORDINAL_POSITION
                """;

            command.Parameters.Add(Parameter(command, "p_schema", schema));

            var current = default(DatabaseTable);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var tableName = reader.GetString(0);

                if (current is null || !string.Equals(current.Name, tableName, StringComparison.Ordinal))
                {
                    // An empty list of the wanted tables means every table of the schema, the way
                    // it means it in EF Core: a list that names a table asks for that table and
                    // only that one.
                    current = wanted.Count == 0 || wanted.Any(t => Matches(t, schema, tableName))
                        ? new DatabaseTable { Database = databaseModel, Schema = schema, Name = tableName }
                        : null;

                    if (current is not null)
                    {
                        databaseModel.Tables.Add(current);
                    }
                }

                current?.Columns.Add(ReadColumn(reader));
            }
        }
    }

    private static DatabaseColumn ReadColumn(DbDataReader reader)
    {
        // DECLARED_DATA_TYPE is the type as written in the DDL ("VARCHAR(50)", "NUMERIC(10, 2)"); DATA_TYPE
        // is only the base name. EF Core's scaffolding model keeps the whole thing in StoreType and has no
        // separate precision or length, so the declared form is what has to be carried.
        // The numbers are the numbers of the columns of the query above: TABLE_NAME,
        // ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, IS_IDENTITY,
        // IDENTITY_GENERATION, IS_GENERATED, GENERATION_EXPRESSION, COLUMN_DEFAULT,
        // DECLARED_DATA_TYPE.
        var column = new DatabaseColumn
        {
            Name = reader.GetString(2),
            StoreType = reader.IsDBNull(10) ? reader.GetString(3) : reader.GetString(10),
            IsNullable = reader.GetString(4) == "YES",
            DefaultValueSql = Text(reader, 9)
        };

        // The columns of the catalogue that say how a column is made are empty when the column
        // is made in the ordinary way, and the reference leaves them empty too.
        if (reader.GetString(5) == "YES")
        {
            column.ValueGenerated = Text(reader, 6) == "ALWAYS"
                ? ValueGenerated.OnAddOrUpdate
                : ValueGenerated.OnAdd;
        }
        else if (reader.GetString(7) == "ALWAYS")
        {
            column.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            column.ComputedColumnSql = Text(reader, 8);
        }

        return column;
    }

    /// <summary>
    ///     The text of a column of the catalogue, or nothing when the column is empty.
    /// </summary>
    /// <param name="reader">The reader over the catalogue.</param>
    /// <param name="ordinal">The number of the column.</param>
    /// <returns>The text, or null.</returns>
    private static string? Text(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void PopulateKeys(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            using var command = connection.CreateCommand();

            // H2 keeps index columns in INFORMATION_SCHEMA.INDEX_COLUMNS rather than on INDEXES, which is
            // why joining only TABLE_CONSTRAINTS to KEY_COLUMN_USAGE would miss a plain CREATE INDEX.
            command.CommandText = """
                SELECT tc.TABLE_NAME, tc.CONSTRAINT_NAME, tc.CONSTRAINT_TYPE, kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                       ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                      AND kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
                WHERE tc.TABLE_SCHEMA = @p_schema
                  AND tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE')
                ORDER BY tc.TABLE_NAME, tc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION
                """;

            command.Parameters.Add(Parameter(command, "p_schema", schema));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
                {
                    continue;
                }

                var tableName = reader.GetString(0);
                var constraintName = reader.GetString(1);
                var constraintType = reader.GetString(2);

                var table = Find(databaseModel, schema, tableName);
                if (table is null)
                {
                    continue;
                }

                if (constraintType == "PRIMARY KEY")
                {
                    table.PrimaryKey ??= new DatabasePrimaryKey { Name = constraintName, Table = table };

                    if (!reader.IsDBNull(3) && Find(table, reader.GetString(3)) is { } keyColumn)
                    {
                        table.PrimaryKey.Columns.Add(keyColumn);
                    }
                }
                else
                {
                    var existing = table.UniqueConstraints.FirstOrDefault(k => k.Name == constraintName);
                    if (existing is null)
                    {
                        existing = new DatabaseUniqueConstraint { Name = constraintName, Table = table };
                        table.UniqueConstraints.Add(existing);
                    }

                    if (!reader.IsDBNull(3) && Find(table, reader.GetString(3)) is { } column)
                    {
                        existing.Columns.Add(column);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Reads the indexes of the tables.
    /// </summary>
    /// <remarks>
    ///     H2 keeps the columns of an index in INFORMATION_SCHEMA.INDEX_COLUMNS rather than on the
    ///     index itself, and the indexes that stand for the primary keys are marked as generated:
    ///     they are the keys, and the keys are read on their own.
    /// </remarks>
    /// <param name="connection">The connection to the database.</param>
    /// <param name="databaseModel">The model to fill.</param>
    /// <param name="schemas">The schemas to read.</param>
    private static void PopulateIndexes(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT i.INDEX_NAME, i.TABLE_NAME, i.INDEX_TYPE_NAME, c.COLUMN_NAME
                FROM INFORMATION_SCHEMA.INDEXES i
                JOIN INFORMATION_SCHEMA.INDEX_COLUMNS c
                     ON c.INDEX_SCHEMA = i.INDEX_SCHEMA
                    AND c.INDEX_NAME = i.INDEX_NAME
                WHERE i.INDEX_SCHEMA = @p_schema
                  AND i.IS_GENERATED = FALSE
                ORDER BY i.INDEX_NAME, c.ORDINAL_POSITION
                """;

            command.Parameters.Add(Parameter(command, "p_schema", schema));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1))
                {
                    continue;
                }

                var indexName = reader.GetString(0);
                var tableName = reader.GetString(1);
                var indexType = reader.IsDBNull(2) ? null : reader.GetString(2);

                var table = Find(databaseModel, schema, tableName);
                if (table is null)
                {
                    continue;
                }

                var index = table.Indexes.FirstOrDefault(i => i.Name == indexName);
                if (index is null)
                {
                    index = new DatabaseIndex
                    {
                        Name = indexName,
                        Table = table,
                        IsUnique = indexType is not null && indexType.StartsWith("UNIQUE", StringComparison.Ordinal)
                    };

                    table.Indexes.Add(index);
                }

                if (!reader.IsDBNull(3) && Find(table, reader.GetString(3)) is { } column)
                {
                    index.Columns.Add(column);
                }
            }
        }
    }

    private static void PopulateSequences(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            using var command = connection.CreateCommand();

            // H2 names these START_VALUE / CYCLE_OPTION; there is no CYCLE and no CACHE_SIZE column, and
            // the base value of a sequence is a separate column from the value it starts at.
            command.CommandText = """
                SELECT SEQUENCE_NAME, DATA_TYPE, START_VALUE, INCREMENT, MINIMUM_VALUE, MAXIMUM_VALUE, CYCLE_OPTION
                FROM INFORMATION_SCHEMA.SEQUENCES
                WHERE SEQUENCE_SCHEMA = @p_schema
                ORDER BY SEQUENCE_NAME
                """;

            command.Parameters.Add(Parameter(command, "p_schema", schema));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                databaseModel.Sequences.Add(
                    new DatabaseSequence
                    {
                        Database = databaseModel,
                        Schema = schema,
                        Name = reader.GetString(0),
                        StoreType = reader.IsDBNull(1) ? null : reader.GetString(1),
                        StartValue = ToInt(reader.GetValue(2)),
                        IncrementBy = ToInt(reader.GetValue(3)),
                        MinValue = ToInt(reader.GetValue(4)),
                        MaxValue = ToInt(reader.GetValue(5)),
                        IsCyclic = !reader.IsDBNull(6) && reader.GetString(6) == "YES"
                    });
            }
        }
    }

    private static void PopulateForeignKeys(DbConnection connection, DatabaseModel databaseModel)
    {
        using var command = connection.CreateCommand();

        // REFERENTIAL_CONSTRAINTS names the referenced constraint, which is how the principal table and its
        // key columns are recovered: H2 exposes no "referenced column" column of its own.
        command.CommandText = """
            SELECT fk.TABLE_SCHEMA, fk.TABLE_NAME, fk.CONSTRAINT_NAME, fk.COLUMN_NAME,
                   rc.UNIQUE_CONSTRAINT_SCHEMA, fk.UNIQUE_CONSTRAINT_NAME,
                   rc.UPDATE_RULE, rc.DELETE_RULE, fk.ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk
            JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                 ON rc.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
                AND rc.CONSTRAINT_SCHEMA = fk.CONSTRAINT_SCHEMA
            ORDER BY fk.TABLE_SCHEMA, fk.TABLE_NAME, fk.CONSTRAINT_NAME, fk.ORDINAL_POSITION
            """;

        var seen = new Dictionary<(string, string, string), DatabaseForeignKey>();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
            {
                continue;
            }

            var schema = reader.GetString(0);
            var tableName = reader.GetString(1);
            var constraintName = reader.GetString(2);

            var table = Find(databaseModel, schema, tableName);
            if (table is null)
            {
                continue;
            }

            if (!seen.TryGetValue((schema, tableName, constraintName), out var fk))
            {
                var principalSchema = reader.IsDBNull(4) ? null : reader.GetString(4);
                var principalConstraint = reader.IsDBNull(5) ? null : reader.GetString(5);

                var principal = principalConstraint is null
                    ? throw new InvalidOperationException(
                        $"Foreign key '{constraintName}' on {schema}.{tableName} names no referenced " +
                        "constraint, so the table it points at cannot be determined.")
                    : Find(databaseModel, principalSchema, PrincipalTableName(connection, principalSchema, principalConstraint))
                        ?? throw new InvalidOperationException(
                            $"Foreign key '{constraintName}' references constraint '{principalConstraint}', " +
                            "which is not among the tables being scaffolded. Include that table or the constraint cannot be resolved.");

                fk = new DatabaseForeignKey
                {
                    Name = constraintName,
                    Table = table,
                    PrincipalTable = principal,
                    OnDelete = ToReferentialAction(reader.IsDBNull(7) ? null : reader.GetString(7))
                };

                table.ForeignKeys.Add(fk);
                seen[(schema, tableName, constraintName)] = fk;

                // OnUpdate is deliberately not read: EF Core's scaffolding model has no member for it,
                // so an H2 ON UPDATE rule has nowhere to go and dropping it silently is better than
                // pretending it was carried. It is a known gap, tracked in docs/DIALECT.md.
                _ = reader.IsDBNull(6) ? null : reader.GetString(6);
            }

            if (!reader.IsDBNull(3) && Find(table, reader.GetString(3)) is { } column)
            {
                fk.Columns.Add(column);
            }
        }
    }

    private static string PrincipalTableName(DbConnection connection, string? schema, string constraintName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
            WHERE CONSTRAINT_NAME = @p_name AND CONSTRAINT_SCHEMA = @p_schema
            """;

        command.Parameters.Add(Parameter(command, "p_name", constraintName));
        command.Parameters.Add(Parameter(command, "p_schema", (object?)schema ?? "PUBLIC"));

        return command.ExecuteScalar() as string
               ?? throw new InvalidOperationException(
                   $"Referenced constraint '{constraintName}' is not present in TABLE_CONSTRAINTS, so the " +
                   "referenced table cannot be resolved.");
    }

    private static DatabaseTable? Find(DatabaseModel model, string? schema, string name)
        => model.Tables.FirstOrDefault(t =>
            string.Equals(t.Schema, schema, StringComparison.Ordinal) &&
            string.Equals(t.Name, name, StringComparison.Ordinal));

    private static DatabaseColumn? Find(DatabaseTable table, string name)
        => table.Columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal));

    private static ReferentialAction ToReferentialAction(string? rule)
        => rule switch
        {
            "CASCADE" => ReferentialAction.Cascade,
            "SET NULL" => ReferentialAction.SetNull,
            "SET DEFAULT" => ReferentialAction.SetDefault,
            "RESTRICT" => ReferentialAction.Restrict,
            _ => ReferentialAction.NoAction
        };

    private static int? ToInt(object value)
        => value is null or DBNull
            ? null
            : Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static long? ToLong(object value)
        => value is null or DBNull
            ? null
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);

    private static bool Matches((string? Schema, string Name) wanted, string schema, string name)
        => (wanted.Schema is null || string.Equals(wanted.Schema, schema, StringComparison.OrdinalIgnoreCase))
           && string.Equals(wanted.Name, name, StringComparison.OrdinalIgnoreCase);

    private static (string? Schema, string Name) Parse(string table)
    {
        var parts = table.Split('.', 2);
        return parts.Length == 1 ? (null, parts[0].Trim('"')) : (parts[0].Trim('"'), parts[1].Trim('"'));
    }

    private static DbParameter Parameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        return parameter;
    }
}
