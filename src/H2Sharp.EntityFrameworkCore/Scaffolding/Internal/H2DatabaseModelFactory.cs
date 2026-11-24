using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
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
///         every query. None of that exists in H2. The shared <see cref="IH2CatalogReader" /> reads
///         INFORMATION_SCHEMA, the standard catalogue the reference engine exposes, and this class
///         interprets its typed rows; see <c>docs/DIALECT.md</c> for the catalogue probes.
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
    private readonly IH2CatalogReader _catalog;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EntityFramework Core release.
    /// </summary>
    public H2DatabaseModelFactory(
        IDiagnosticsLogger<DbLoggerCategory.Scaffolding> logger,
        IH2CatalogReader? catalog = null)
    {
        _logger = Check.NotNull(logger, nameof(logger));
        // Direct construction in tests does not go through EF's design-time container.
        _catalog = catalog ?? new H2CatalogReader();
    }

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
            if (!_catalog.IsAvailable(dbConnection))
            {
                throw new InvalidOperationException(
                    "Scaffolding reads INFORMATION_SCHEMA, and this H2 database does not expose it. The " +
                    "reference engine (H2 2.5) provides it.");
            }

            var databaseModel = new DatabaseModel
            {
                DatabaseName = dbConnection.Database,
                DefaultSchema = "PUBLIC"
            };

            var catalog = _catalog.GetSchemas(dbConnection);
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
            PopulateCheckConstraints(dbConnection, databaseModel, schemas);
            PopulateSequences(dbConnection, databaseModel, schemas);
            PopulateEnums(dbConnection, databaseModel, schemas);
            PopulateForeignKeys(dbConnection, databaseModel);

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

    private void PopulateTables(
        DbConnection connection,
        DatabaseModel databaseModel,
        IReadOnlyList<string> schemas,
        IReadOnlyList<(string? Schema, string Name)> wanted)
    {
        foreach (var schema in schemas)
        {
            var tableTypes = new Dictionary<string, string>(StringComparer.Ordinal);
            var tableComments = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var table in _catalog.GetTables(connection, schema))
            {
                tableTypes[table.Name] = table.Type ?? "";
                if (table.Comment is not null)
                {
                    tableComments[table.Name] = table.Comment;
                }
            }

            var current = default(DatabaseTable);

            foreach (var column in _catalog.GetColumns(connection, schema))
            {
                var tableName = column.TableName;

                if (current is null || !string.Equals(current.Name, tableName, StringComparison.Ordinal))
                {
                    // An empty list of the wanted tables means every table of the schema, the way
                    // it means it in EF Core: a list that names a table asks for that table and
                    // only that one.
                    var wantedTable = wanted.Count == 0 || wanted.Any(t => Matches(t, schema, tableName));
                    var isView = tableTypes.TryGetValue(tableName, out var tableType) && tableType == "VIEW";

                    // A materialized view exposes its backing table as NAME$<n>; it is an
                    // implementation detail and must not become a second scaffolded table.
                    var backingOf = tableName.LastIndexOf('$');
                    var isViewBacking = !isView && backingOf > 0
                        && int.TryParse(tableName[(backingOf + 1)..], out _)
                        && tableTypes.TryGetValue(tableName[..backingOf], out var ownerType)
                        && ownerType == "VIEW";

                    current = wantedTable && !isViewBacking
                        ? isView
                            ? new DatabaseView
                            {
                                Database = databaseModel,
                                Schema = schema,
                                Name = tableName,
                                Comment = tableComments.GetValueOrDefault(tableName)
                            }
                            : new DatabaseTable
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

                // An enum column is reported as DATA_TYPE = ENUM, and its type name lives in
                // DOMAIN_SCHEMA/DOMAIN_NAME. EF Core has no scaffolding model for a database enum
                // type, so the column is left out and the warning says the property must be mapped
                // by hand; dropping it silently would lose a column without a trace.
                if (column.DataType == "ENUM")
                {
                    _logger.EnumColumnSkippedWarning(
                        $"{current.Schema ?? databaseModel.DefaultSchema}.{current.Name}.{column.ColumnName}");
                    continue;
                }

                var scaffolded = ReadColumn(column);
                scaffolded.Table = current;
                current.Columns.Add(scaffolded);
            }
        }
    }

    private static DatabaseColumn ReadColumn(H2ColumnRow row)
    {
        // DECLARED_DATA_TYPE is the type as written in the DDL ("VARCHAR(50)", "NUMERIC(10, 2)"); DATA_TYPE
        // is only the base name. EF Core's scaffolding model keeps the whole thing in StoreType and has no
        // separate precision or length, so the declared form is what has to be carried.
        var column = new DatabaseColumn
        {
            Name = row.ColumnName,
            StoreType = StoreType(row),
            IsNullable = row.IsNullable == "YES",
            DefaultValueSql = row.ColumnDefault,
            Comment = row.Remarks
        };

        // The columns of the catalogue that say how a column is made are empty when the column
        // is made in the ordinary way, and the reference leaves them empty too.
        if (row.IsIdentity == "YES")
        {
            var generation = row.IdentityGeneration;
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
            var start = row.IdentityStart;
            var min = row.IdentityMinimum;
            var max = row.IdentityMaximum;
            var cache = row.IdentityCache;

            var options = new IdentitySequenceOptionsData
            {
                StartValue = start == 1 ? null : start,
                IncrementBy = row.IdentityIncrement ?? 1,
                MinValue = min == 1 ? null : min,
                MaxValue = max is 2147483647 or 9223372036854775807 ? null : max,
                IsCyclic = row.IdentityCycle is "YES" or "TRUE",
                NumbersToCache = cache is null or 1 or 32 ? 1 : cache.Value
            };

            if (options.StartValue is not null || options.IncrementBy != 1
                || options.MinValue is not null || options.MaxValue is not null
                || options.IsCyclic || options.NumbersToCache != 1)
            {
                column[H2AnnotationNames.IdentityOptions] = options.Serialize();
            }
        }
        else if (row.IsGenerated == "ALWAYS")
        {
            column.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            column.ComputedColumnSql = row.GenerationExpression;

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

    private static string StoreType(H2ColumnRow row)
    {
        var declared = row.DeclaredDataType;
        if (declared is not null)
        {
            // NUMERIC carries its facets in DECLARED_NUMERIC_PRECISION/SCALE, not in the
            // declared type name; a bare NUMERIC means the default precision with scale 0.
            if (!declared.Contains('(') && row.DeclaredNumericPrecision is not null)
            {
                var scale = row.DeclaredNumericScale ?? 0;
                return Normalize(
                    $"{declared}({row.DeclaredNumericPrecision.Value.ToString(CultureInfo.InvariantCulture)}," +
                    $"{scale.ToString(CultureInfo.InvariantCulture)})");
            }

            return Normalize(declared);
        }

        // H2 leaves DECLARED_DATA_TYPE empty for the common types and keeps the length or
        // precision in the separate catalogue columns. EF's DatabaseColumn has a single
        // StoreType string, so the full type has to be assembled here.
        var type = row.DataType ?? string.Empty;

        if (row.CharacterMaximumLength is not null)
        {
            var length = row.CharacterMaximumLength.Value;

            // H2 reports its maximum character length (1_000_000_000) for an unbounded
            // CHARACTER VARYING, not an explicit precision written by the user.
            return Normalize(length == 1_000_000_000
                ? type
                : $"{type}({length.ToString(CultureInfo.InvariantCulture)})");
        }

        if (row.DateTimePrecision is not null)
        {
            var precision = row.DateTimePrecision.Value.ToString(CultureInfo.InvariantCulture);
            const string withZone = " WITH TIME ZONE";

            return Normalize(type.EndsWith(withZone, StringComparison.Ordinal)
                ? $"{type[..^withZone.Length]}({precision}){withZone}"
                : $"{type}({precision})");
        }

        if (row.NumericPrecision is not null)
        {
            var scale = row.NumericScale ?? 0;
            return Normalize(
                $"{type}({row.NumericPrecision.Value.ToString(CultureInfo.InvariantCulture)}, "
                + $"{scale.ToString(CultureInfo.InvariantCulture)})");
        }

        return Normalize(type);
    }

    private void PopulateKeys(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            foreach (var key in _catalog.GetKeys(connection, schema))
            {
                if (key.TableName is null || key.ConstraintName is null || key.ConstraintType is null)
                {
                    continue;
                }

                var table = Find(databaseModel, schema, key.TableName);
                if (table is null)
                {
                    continue;
                }

                if (key.ConstraintType == "PRIMARY KEY")
                {
                    table.PrimaryKey ??= new DatabasePrimaryKey { Name = key.ConstraintName, Table = table };

                    if (key.ColumnName is not null && Find(table, key.ColumnName) is { } keyColumn)
                    {
                        table.PrimaryKey.Columns.Add(keyColumn);
                    }
                }
                else
                {
                    var existing = table.UniqueConstraints.FirstOrDefault(k => k.Name == key.ConstraintName);
                    if (existing is null)
                    {
                        existing = new DatabaseUniqueConstraint { Name = key.ConstraintName, Table = table };
                        table.UniqueConstraints.Add(existing);
                    }

                    if (key.ColumnName is not null && Find(table, key.ColumnName) is { } column)
                    {
                        existing.Columns.Add(column);
                    }
                }
            }
        }

        // A key over enum-typed columns loses every column when those columns are skipped;
        // an empty key is not a key and must not reach EF's model.
        foreach (var table in databaseModel.Tables)
        {
            if (table.PrimaryKey is { Columns.Count: 0 })
            {
                table.PrimaryKey = null;
            }

            for (var i = table.UniqueConstraints.Count - 1; i >= 0; i--)
            {
                if (table.UniqueConstraints[i].Columns.Count == 0)
                {
                    table.UniqueConstraints.RemoveAt(i);
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
    private void PopulateIndexes(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            foreach (var row in _catalog.GetIndexes(connection, schema))
            {
                if (row.IndexName is null || row.TableName is null)
                {
                    continue;
                }

                var table = Find(databaseModel, schema, row.TableName);
                if (table is null)
                {
                    continue;
                }

                var index = table.Indexes.FirstOrDefault(i => i.Name == row.IndexName);
                if (index is null)
                {
                    index = new DatabaseIndex
                    {
                        Name = row.IndexName,
                        Table = table,
                        IsUnique = row.IndexTypeName is not null
                            && row.IndexTypeName.StartsWith("UNIQUE", StringComparison.Ordinal)
                    };

                    table.Indexes.Add(index);
                }

                if (row.ColumnName is not null && Find(table, row.ColumnName) is { } column)
                {
                    // In a unique index H2 marks covered (INCLUDE) columns with IS_UNIQUE = FALSE;
                    // they are not keys and must stay out of the scaffolded column list.
                    if (index.IsUnique && row.IsUnique is false)
                    {
                        var included = index[H2AnnotationNames.IndexInclude] as string[] ?? Array.Empty<string>();
                        index[H2AnnotationNames.IndexInclude] = new List<string>(included) { column.Name }.ToArray();
                        continue;
                    }

                    index.Columns.Add(column);

                    var descending = index[H2AnnotationNames.IndexSortOrder] as List<bool> ?? new List<bool>();
                    var nullSortOrders = index[H2AnnotationNames.IndexNullSortOrder] as NullSortOrder[]
                        ?? Array.Empty<NullSortOrder>();

                    descending.Add(row.OrderingSpecification == "DESC");
                    nullSortOrders = [.. nullSortOrders, ReadNullSortOrder(row.NullOrdering)];

                    index.IsDescending = descending;
                    index[H2AnnotationNames.IndexSortOrder] = descending;
                    index[H2AnnotationNames.IndexNullSortOrder] = nullSortOrders;
                }
            }
        }

        // An index whose every column was skipped (enum-typed) has no columns and no meaning.
        foreach (var table in databaseModel.Tables)
        {
            for (var i = table.Indexes.Count - 1; i >= 0; i--)
            {
                if (table.Indexes[i].Columns.Count == 0)
                {
                    table.Indexes.RemoveAt(i);
                }
            }
        }
    }

    private void PopulateCheckConstraints(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        // EF Core 9's DatabaseModel has no check-constraint collection. The H2 annotation is copied
        // from the table to the entity type by RelationalScaffoldingModelFactory, and H2's code
        // generator turns each annotation into a HasCheckConstraint(...) call in the scaffold.
        foreach (var schema in schemas)
        {
            foreach (var check in _catalog.GetCheckConstraints(connection, schema))
            {
                var table = Find(databaseModel, schema, check.TableName);
                if (table is not null)
                {
                    table[H2AnnotationNames.CheckConstraintPrefix + check.Name] = check.Clause;
                }
            }
        }
    }

    private void PopulateSequences(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            foreach (var row in _catalog.GetSequences(connection, schema))
            {
                var storeType = row.DataType is null ? null : Normalize(row.DataType);
                var start = row.StartValue;
                var increment = row.Increment;
                var min = row.MinimumValue;
                var max = row.MaximumValue;
                var isCyclic = row.IsCyclic;

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
                        Name = row.Name,
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

    private void PopulateEnums(DbConnection connection, DatabaseModel databaseModel, IReadOnlyList<string> schemas)
    {
        foreach (var schema in schemas)
        {
            var labels = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var row in _catalog.GetEnums(connection, schema))
            {
                if (!labels.TryGetValue(row.Name, out var values))
                {
                    values = [];
                    labels.Add(row.Name, values);
                }

                values.Add(row.Value);
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

    private void PopulateForeignKeys(DbConnection connection, DatabaseModel databaseModel)
    {
        var seen = new Dictionary<(string, string, string), DatabaseForeignKey>();

        foreach (var row in _catalog.GetForeignKeys(connection))
        {
            if (row.TableSchema is null || row.TableName is null || row.ConstraintName is null)
            {
                continue;
            }

            var schema = row.TableSchema;
            var tableName = row.TableName;
            var constraintName = row.ConstraintName;

            var table = Find(databaseModel, schema, tableName);
            if (table is null)
            {
                continue;
            }

            if (!seen.TryGetValue((schema, tableName, constraintName), out var fk))
            {
                var principalSchema = row.UniqueConstraintSchema;
                var principalConstraint = row.UniqueConstraintName;

                if (principalConstraint is null)
                {
                    _logger.ForeignKeyReferencesMissingPrincipalTableWarning(
                        constraintName, schema + "." + tableName, null);
                    continue;
                }

                var principalName = _catalog.GetPrincipalTableName(connection, principalSchema, principalConstraint)
                    ?? throw new InvalidOperationException(
                        $"Referenced constraint '{principalConstraint}' is not present in TABLE_CONSTRAINTS, so the " +
                        "referenced table cannot be resolved.");

                var principal = Find(databaseModel, principalSchema, principalName);

                if (principal is null)
                {
                    _logger.ForeignKeyReferencesMissingPrincipalTableWarning(
                        constraintName,
                        schema + "." + tableName,
                        (principalSchema ?? databaseModel.DefaultSchema) + "." + principalName);
                    continue;
                }

                fk = new DatabaseForeignKey
                {
                    Name = constraintName,
                    Table = table,
                    PrincipalTable = principal,
                    OnDelete = ToReferentialAction(row.DeleteRule)
                };

                foreach (var columnName in _catalog.GetPrincipalColumns(connection, principalSchema, principalConstraint))
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
                _ = row.UpdateRule;
            }

            if (row.ColumnName is not null && Find(table, row.ColumnName) is { } column)
            {
                fk.Columns.Add(column);
            }
        }
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

    private static NullSortOrder ReadNullSortOrder(string? value)
        => value switch
        {
            "FIRST" => NullSortOrder.NullsFirst,
            "LAST" => NullSortOrder.NullsLast,
            _ => NullSortOrder.Unspecified
        };

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

}
