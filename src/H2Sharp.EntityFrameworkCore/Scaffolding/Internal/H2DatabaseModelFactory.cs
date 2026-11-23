using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Storage;
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

        // The database belongs to a data source, and the connection string names the data source
        // the provider keeps for it: the scaffolding then reads the same database the contexts of
        // that connection string talk to.
        var dataSource = H2DataSources.GetOrCreate(connectionString);
        using var connection = dataSource.CreateConnection();
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

            var catalog = GetSchemas(dbConnection);
            var schemas = options.Schemas.Any()
                ? ResolveSchemas(catalog, options.Schemas)
                : catalog;
            var tables = options.Tables.Select(Parse).ToList();

            if (options.Schemas.Any())
            {
                foreach (var requested in options.Schemas)
                {
                    if (!catalog.Contains(requested, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.MissingSchemaWarning(requested);
                    }
                }
            }

            PopulateTables(dbConnection, databaseModel, schemas, tables);
            PopulateKeys(dbConnection, databaseModel, schemas);
            PopulateIndexes(dbConnection, databaseModel, schemas);
            PopulateSequences(dbConnection, databaseModel, schemas);
            PopulateEnums(dbConnection, databaseModel, schemas);
            PopulateForeignKeys(dbConnection, databaseModel, _logger);

            if (options.Tables.Any())
            {
                foreach (var (schema, name) in tables)
                {
                    if (!databaseModel.Tables.Any(
                            t => Matches((schema, name), t.Schema ?? databaseModel.DefaultSchema!, t.Name)))
                    {
                        _logger.MissingTableWarning(name);
                    }
                }
            }

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

    private static List<string> ResolveSchemas(IReadOnlyList<string> catalog, IEnumerable<string> requested)
    {
        var resolved = new List<string>();

        foreach (var schema in requested)
        {
            // An unquoted identifier in H2 is upper-cased; a filter such as "db2" must find
            // DB2 even when a quoted "db2" schema also exists. The upper-case catalogue name
            // wins; the lower-case spelling is used only when no upper-case schema exists.
            var upper = schema.ToUpperInvariant();
            resolved.Add(
                catalog.Contains(upper, StringComparer.Ordinal)
                    ? upper
                    : catalog.Contains(schema, StringComparer.Ordinal)
                        ? schema
                        : catalog.FirstOrDefault(s => string.Equals(s, schema, StringComparison.OrdinalIgnoreCase))
                          ?? schema);
        }

        return resolved;
    }

    private static List<string> GetSchemas(DbConnection connection)
    {
        var schemas = new List<string>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME <> 'INFORMATION_SCHEMA'";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                schemas.Add(reader.GetString(0));
            }
        }

        // H2's reference catalogue always contains PUBLIC; keep scaffolds usable even when
        // a custom catalogue doesn't expose the SCHEMATA view.
        if (!schemas.Contains("PUBLIC"))
        {
            schemas.Add("PUBLIC");
        }

        return schemas;
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
            var tableComments = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var comments = connection.CreateCommand())
            {
                comments.CommandText =
                    "SELECT TABLE_NAME, REMARKS FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = $p_schema";
                comments.Parameters.Add(Parameter(comments, "p_schema", schema));

                using var remarks = comments.ExecuteReader();
                while (remarks.Read())
                {
                    if (!remarks.IsDBNull(1))
                    {
                        tableComments[remarks.GetString(0)] = remarks.GetString(1);
                    }
                }
            }

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
                       DECLARED_DATA_TYPE,
                       REMARKS,
                       CHARACTER_MAXIMUM_LENGTH,
                       NUMERIC_PRECISION,
                       NUMERIC_SCALE,
                       DATETIME_PRECISION,
                       DECLARED_NUMERIC_PRECISION,
                       DECLARED_NUMERIC_SCALE,
                       IDENTITY_START,
                       IDENTITY_INCREMENT,
                       IDENTITY_MINIMUM,
                       IDENTITY_MAXIMUM,
                       IDENTITY_CYCLE,
                       IDENTITY_CACHE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = $p_schema
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
                        ? new DatabaseTable
                    {
                        Database = databaseModel,
                        Schema = schema,
                        Name = tableName,
                        Comment = tableComments.GetValueOrDefault(tableName)
                    }
                        : null;

                    if (current is not null)
                    {
                        databaseModel.Tables.Add(current);
                    }
                }

                if (current is null)
                {
                    continue;
                }

                var column = ReadColumn(reader);
                column.Table = current;
                current.Columns.Add(column);
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
        // DECLARED_DATA_TYPE, REMARKS, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION,
        // NUMERIC_SCALE, DATETIME_PRECISION.
        var column = new DatabaseColumn
        {
            Name = reader.GetString(2),
            StoreType = StoreType(reader),
            IsNullable = reader.GetString(4) == "YES",
            DefaultValueSql = Text(reader, 9),
            Comment = Text(reader, 11)
        };

        // The columns of the catalogue that say how a column is made are empty when the column
        // is made in the ordinary way, and the reference leaves them empty too.
        if (reader.GetString(5) == "YES")
        {
            var generation = Text(reader, 6);
            column.ValueGenerated = generation == "ALWAYS"
                ? ValueGenerated.OnAddOrUpdate
                : ValueGenerated.OnAdd;

            // Keep the H2-specific strategy on the scaffolded column: migrations compare it with
            // the model's UseIdentityByDefaultColumn/UseIdentityAlwaysColumn setting, and a plain
            // ValueGenerated flag loses the difference between BY DEFAULT and ALWAYS.
            column[H2AnnotationNames.ValueGenerationStrategy] = generation == "ALWAYS"
                ? H2ValueGenerationStrategy.IdentityAlwaysColumn
                : H2ValueGenerationStrategy.IdentityByDefaultColumn;

            // H2 materialises defaults for every identity sequence; EF's model keeps the
            // corresponding options as "not specified". Strip only those materialised values.
            var start = ToLong(reader.GetValue(18));
            var min = ToLong(reader.GetValue(20));
            var max = ToLong(reader.GetValue(21));
            var cache = ToLong(reader.GetValue(23));

            var options = new IdentitySequenceOptionsData
            {
                StartValue = start == 1 ? null : start,
                IncrementBy = ToLong(reader.GetValue(19)) ?? 1,
                MinValue = min == 1 ? null : min,
                MaxValue = max is 2147483647 or 9223372036854775807 ? null : max,
                IsCyclic = reader.GetValue(22)?.ToString() is "YES" or "TRUE",
                NumbersToCache = cache is null or 1 or 32 ? 1 : cache.Value
            };

            if (options.StartValue is not null || options.IncrementBy != 1
                || options.MinValue is not null || options.MaxValue is not null
                || options.IsCyclic || options.NumbersToCache != 1)
            {
                column[H2AnnotationNames.IdentityOptions] = options.Serialize();
            }
        }
        else if (reader.GetString(7) == "ALWAYS")
        {
            column.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            column.ComputedColumnSql = Text(reader, 8);

            // H2 computed columns are stored in 2.5; it has no separate VIRTUAL spelling.
            column.IsStored = true;
        }

        return column;
    }

    // H2's catalogue spells the common types as CHARACTER VARYING / CHARACTER / BIGINT.
    // EF's store types follow the spellings the provider writes into DDL, so the two have
    // to be brought together here; H2 accepts both spellings and reports only one.
    private static string Normalize(string type)
        => type.ToLowerInvariant()
            .Replace("character varying", "varchar", StringComparison.Ordinal)
            .Replace("character", "char", StringComparison.Ordinal);

    private static string StoreType(DbDataReader reader)
    {
        var declared = Text(reader, 10);
        if (declared is not null)
        {
            // NUMERIC carries its facets in DECLARED_NUMERIC_PRECISION/SCALE, not in the
            // declared type name; a bare NUMERIC means the default precision with scale 0.
            if (!declared.Contains('(') && !reader.IsDBNull(16))
            {
                var scale = reader.IsDBNull(17) ? 0 : reader.GetInt32(17);
                return Normalize(
                    $"{declared}({reader.GetInt32(16).ToString(CultureInfo.InvariantCulture)}," +
                    $"{scale.ToString(CultureInfo.InvariantCulture)})");
            }

            return Normalize(declared);
        }

        // H2 leaves DECLARED_DATA_TYPE empty for the common types and keeps the length or
        // precision in the separate catalogue columns. EF's DatabaseColumn has a single
        // StoreType string, so the full type has to be assembled here.
        var type = reader.GetString(3);

        if (!reader.IsDBNull(12))
        {
            var length = reader.GetInt64(12);

            // H2 reports its maximum character length (1_000_000_000) for an unbounded
            // CHARACTER VARYING, not an explicit precision written by the user.
            return Normalize(length == 1_000_000_000
                ? type
                : $"{type}({length.ToString(CultureInfo.InvariantCulture)})");
        }

        if (!reader.IsDBNull(15))
        {
            return Normalize($"{type}({reader.GetInt32(15).ToString(CultureInfo.InvariantCulture)})");
        }

        if (!reader.IsDBNull(13))
        {
            var scale = reader.IsDBNull(14) ? 0 : reader.GetInt32(14);
            return Normalize($"{type}({reader.GetInt32(13).ToString(CultureInfo.InvariantCulture)}, {scale.ToString(CultureInfo.InvariantCulture)})");
        }

        return Normalize(type);
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
                WHERE tc.TABLE_SCHEMA = $p_schema
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
                SELECT i.INDEX_NAME, i.TABLE_NAME, i.INDEX_TYPE_NAME, c.COLUMN_NAME,
                       c.ORDERING_SPECIFICATION, c.NULL_ORDERING, c.IS_UNIQUE
                FROM INFORMATION_SCHEMA.INDEXES i
                JOIN INFORMATION_SCHEMA.INDEX_COLUMNS c
                     ON c.INDEX_SCHEMA = i.INDEX_SCHEMA
                    AND c.INDEX_NAME = i.INDEX_NAME
                WHERE i.INDEX_SCHEMA = $p_schema
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
                    // In a unique index H2 marks covered (INCLUDE) columns with IS_UNIQUE = FALSE;
                    // they are not keys and must stay out of the scaffolded column list.
                    if (index.IsUnique && !reader.IsDBNull(6) && !reader.GetBoolean(6))
                    {
                        var included = index[H2AnnotationNames.IndexInclude] as string[] ?? Array.Empty<string>();
                        index[H2AnnotationNames.IndexInclude] = new List<string>(included) { column.Name }.ToArray();
                        continue;
                    }

                    index.Columns.Add(column);

                    var descending = index[H2AnnotationNames.IndexSortOrder] as List<bool> ?? new List<bool>();
                    var nullSortOrders = index[H2AnnotationNames.IndexNullSortOrder] as NullSortOrder[]
                        ?? Array.Empty<NullSortOrder>();

                    descending.Add(reader.GetString(4) == "DESC");
                    nullSortOrders = [.. nullSortOrders, ReadNullSortOrder(reader)];

                    index.IsDescending = descending;
                    index[H2AnnotationNames.IndexSortOrder] = descending;
                    index[H2AnnotationNames.IndexNullSortOrder] = nullSortOrders;
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
                WHERE SEQUENCE_SCHEMA = $p_schema
                ORDER BY SEQUENCE_NAME
                """;

            command.Parameters.Add(Parameter(command, "p_schema", schema));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var storeType = reader.IsDBNull(1) ? null : Normalize(reader.GetString(1));
                var start = ToLong(reader.GetValue(2));
                var increment = ToInt(reader.GetValue(3));
                var min = ToLong(reader.GetValue(4));
                var max = ToLong(reader.GetValue(5));
                var isCyclic = !reader.IsDBNull(6) && reader.GetString(6) == "YES";

                // H2 materialises every default. Like the reference provider, report no
                // options at all for an ordinary sequence, but keep a start value when any
                // other option makes the sequence non-default.
                var defaultMax = storeType switch
                {
                    "smallint" => (long)short.MaxValue,
                    "integer" => int.MaxValue,
                    _ => long.MaxValue
                };
                var hasNonDefault = start != 1 || increment != 1 || min != 1 || max != defaultMax || isCyclic;

                databaseModel.Sequences.Add(
                    new DatabaseSequence
                    {
                        Database = databaseModel,
                        Schema = schema,
                        Name = reader.GetString(0),
                        StoreType = storeType,
                        StartValue = hasNonDefault ? start : null,
                        IncrementBy = hasNonDefault ? increment : null,
                        MinValue = hasNonDefault ? min : null,
                        MaxValue = hasNonDefault ? max : null,
                        IsCyclic = isCyclic ? true : hasNonDefault ? false : null
                    });
            }
        }
    }

    private static void PopulateEnums(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT d.DOMAIN_NAME, v.VALUE_NAME
                FROM INFORMATION_SCHEMA.DOMAINS d
                JOIN INFORMATION_SCHEMA.ENUM_VALUES v
                     ON v.OBJECT_SCHEMA = d.DOMAIN_SCHEMA
                    AND v.OBJECT_NAME = d.DOMAIN_NAME
                    AND v.OBJECT_TYPE = 'DOMAIN'
                WHERE d.DOMAIN_SCHEMA = $p_schema AND d.DATA_TYPE = 'ENUM'
                ORDER BY d.DOMAIN_NAME, v.VALUE_ORDINAL
                """;

            command.Parameters.Add(Parameter(command, "p_schema", schema));

            var labels = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    if (!labels.TryGetValue(name, out var values))
                    {
                        values = [];
                        labels.Add(name, values);
                    }

                    values.Add(reader.GetString(1));
                }
            }

            foreach (var (name, values) in labels)
            {
                // EF models use a null schema for the default one; H2's catalogue names it PUBLIC.
                H2Enum.GetOrAddH2Enum(
                    databaseModel,
                    string.Equals(schema, databaseModel.DefaultSchema, StringComparison.Ordinal) ? null : schema,
                    name,
                    values.ToArray());
            }
        }
    }

    private static void PopulateForeignKeys(
        DbConnection connection,
        DatabaseModel databaseModel,
        IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger)
    {
        using var command = connection.CreateCommand();

        // REFERENTIAL_CONSTRAINTS names the referenced constraint, which is how the principal table and its
        // key columns are recovered: H2 exposes no "referenced column" column of its own.
        command.CommandText = """
            SELECT fk.TABLE_SCHEMA, fk.TABLE_NAME, fk.CONSTRAINT_NAME, fk.COLUMN_NAME,
                   rc.UNIQUE_CONSTRAINT_SCHEMA, rc.UNIQUE_CONSTRAINT_NAME,
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

                if (principalConstraint is null)
                {
                    logger.ForeignKeyReferencesMissingPrincipalTableWarning(
                        constraintName, schema + "." + tableName, null);
                    continue;
                }

                var principal = Find(
                    databaseModel,
                    principalSchema,
                    PrincipalTableName(connection, principalSchema, principalConstraint));

                if (principal is null)
                {
                    logger.ForeignKeyReferencesMissingPrincipalTableWarning(
                        constraintName,
                        schema + "." + tableName,
                        (principalSchema ?? databaseModel.DefaultSchema) + "." +
                        PrincipalTableName(connection, principalSchema, principalConstraint));
                    continue;
                }

                fk = new DatabaseForeignKey
                {
                    Name = constraintName,
                    Table = table,
                    PrincipalTable = principal,
                    OnDelete = ToReferentialAction(reader.IsDBNull(7) ? null : reader.GetString(7))
                };

                foreach (var columnName in PrincipalColumns(connection, principalSchema, principalConstraint))
                {
                    if (Find(principal, columnName) is { } principalColumn)
                    {
                        fk.PrincipalColumns.Add(principalColumn);
                    }
                }

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
            WHERE CONSTRAINT_NAME = $p_name AND CONSTRAINT_SCHEMA = $p_schema
            """;

        command.Parameters.Add(Parameter(command, "p_name", constraintName));
        command.Parameters.Add(Parameter(command, "p_schema", (object?)schema ?? "PUBLIC"));

        return command.ExecuteScalar() as string
               ?? throw new InvalidOperationException(
                   $"Referenced constraint '{constraintName}' is not present in TABLE_CONSTRAINTS, so the " +
                   "referenced table cannot be resolved.");
    }

    private static List<string> PrincipalColumns(DbConnection connection, string? schema, string constraintName)
    {
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE CONSTRAINT_NAME = $p_name AND CONSTRAINT_SCHEMA = $p_schema
            ORDER BY ORDINAL_POSITION
            """;

        command.Parameters.Add(Parameter(command, "p_name", constraintName));
        command.Parameters.Add(Parameter(command, "p_schema", (object?)schema ?? "PUBLIC"));

        var names = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
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

    private static NullSortOrder ReadNullSortOrder(DbDataReader reader)
        => reader.IsDBNull(5)
            ? NullSortOrder.Unspecified
            : reader.GetString(5) switch
            {
                "FIRST" => NullSortOrder.NullsFirst,
                "LAST" => NullSortOrder.NullsLast,
                _ => NullSortOrder.Unspecified
            };

    private static long? ToLong(object value)
        => value is null or DBNull
            ? null
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);

    private static bool Matches((string? Schema, string Name) wanted, string schema, string name)
        => (wanted.Schema is null || string.Equals(wanted.Schema, schema, StringComparison.OrdinalIgnoreCase))
           && string.Equals(wanted.Name, name, StringComparison.OrdinalIgnoreCase);

    private static (string? Schema, string Name) Parse(string table)
    {
        var first = ReadIdentifier(table, 0, out var index);
        if (index < table.Length && table[index] == '.')
        {
            var second = ReadIdentifier(table, index + 1, out _);
            return (first, second);
        }

        return (null, first);
    }

    private static string ReadIdentifier(string text, int start, out int end)
    {
        if (start < text.Length && text[start] == '"')
        {
            var value = new StringBuilder();
            var i = start + 1;
            while (i < text.Length)
            {
                if (text[i] == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        value.Append('"');
                        i += 2;
                        continue;
                    }

                    i++;
                    break;
                }

                value.Append(text[i++]);
            }

            end = i;
            return value.ToString();
        }

        var dot = text.IndexOf('.', start);
        end = dot < 0 ? text.Length : dot;
        return text[start..end].Trim();
    }

    private static DbParameter Parameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        return parameter;
    }
}
