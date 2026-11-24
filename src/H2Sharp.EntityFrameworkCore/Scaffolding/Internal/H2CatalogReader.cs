/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MPL 2.0 or the EPL 1.0 (see LICENSE-H2.txt).
 *
 * Every query below was taken from the pinned H2 2.5 reference catalogue, not from
 * EF's pg_catalog-based upstream provider. In particular index columns live in
 * INDEX_COLUMNS, identity facets live in COLUMNS, and enum labels live in
 * DOMAINS joined to ENUM_VALUES. The row shapes are fixed; the interpretation of
 * those rows is the caller's job.
 */
using System.Data.Common;
using System.Globalization;

namespace H2Sharp.EntityFrameworkCore.Scaffolding.Internal;

internal sealed class H2CatalogReader : IH2CatalogReader
{
    public bool IsAvailable(DbConnection connection)
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

    public IReadOnlyList<string> GetSchemas(DbConnection connection)
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
        // a custom catalogue does not expose the SCHEMATA view.
        if (!schemas.Contains("PUBLIC"))
        {
            schemas.Add("PUBLIC");
        }

        return schemas;
    }

    public IReadOnlyList<H2TableRow> GetTables(DbConnection connection, string schema)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT TABLE_NAME, TABLE_TYPE, REMARKS FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = $p_schema";
        command.Parameters.Add(Parameter(command, "p_schema", schema));

        var tables = new List<H2TableRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(
                new H2TableRow(
                    reader.GetString(0),
                    Text(reader, 1) ?? "",
                    Text(reader, 2)));
        }

        return tables;
    }

    public IReadOnlyList<H2ColumnRow> GetColumns(DbConnection connection, string schema)
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

        var columns = new List<H2ColumnRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(
                new H2ColumnRow(
                    reader.GetString(0),
                    reader.GetString(2),
                    Text(reader, 3),
                    Text(reader, 4) ?? "",
                    Text(reader, 5) ?? "",
                    Text(reader, 6),
                    Text(reader, 7) ?? "",
                    Text(reader, 8),
                    Text(reader, 9),
                    Text(reader, 10),
                    Text(reader, 11),
                    Long(reader, 12),
                    Int(reader, 13),
                    Int(reader, 14),
                    Int(reader, 15),
                    Int(reader, 16),
                    Int(reader, 17),
                    Long(reader, 18),
                    Long(reader, 19),
                    Long(reader, 20),
                    Long(reader, 21),
                    Text(reader, 22),
                    Long(reader, 23)));
        }

        return columns;
    }

    public IReadOnlyList<H2KeyRow> GetKeys(DbConnection connection, string schema)
    {
        using var command = connection.CreateCommand();

        // H2 keeps index columns in INDEX_COLUMNS rather than on INDEXES, which is why
        // joining only TABLE_CONSTRAINTS to KEY_COLUMN_USAGE would miss a plain CREATE INDEX.
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

        var keys = new List<H2KeyRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            keys.Add(
                new H2KeyRow(
                    Text(reader, 0),
                    Text(reader, 1),
                    Text(reader, 2),
                    Text(reader, 3)));
        }

        return keys;
    }

    public IReadOnlyList<H2IndexRow> GetIndexes(DbConnection connection, string schema)
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

        var indexes = new List<H2IndexRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            indexes.Add(
                new H2IndexRow(
                    Text(reader, 0),
                    Text(reader, 1),
                    Text(reader, 2),
                    Text(reader, 3),
                    Text(reader, 4),
                    Text(reader, 5),
                    reader.IsDBNull(6) ? null : reader.GetBoolean(6)));
        }

        return indexes;
    }

    public IReadOnlyList<H2CheckConstraintRow> GetCheckConstraints(DbConnection connection, string schema)
    {
        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT tc.TABLE_NAME, tc.CONSTRAINT_NAME, cc.CHECK_CLAUSE
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS cc
                 ON cc.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                AND cc.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
            WHERE tc.TABLE_SCHEMA = $p_schema AND tc.CONSTRAINT_TYPE = 'CHECK'
            ORDER BY tc.TABLE_NAME, tc.CONSTRAINT_NAME
            """;

        command.Parameters.Add(Parameter(command, "p_schema", schema));

        var checks = new List<H2CheckConstraintRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            checks.Add(
                new H2CheckConstraintRow(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2)));
        }

        return checks;
    }

    public IReadOnlyList<H2SequenceRow> GetSequences(DbConnection connection, string schema)
    {
        using var command = connection.CreateCommand();

        // H2 names these START_VALUE / CYCLE_OPTION; there is no CYCLE and no CACHE_SIZE column,
        // and the base value of a sequence is a separate column from the value it starts at.
        command.CommandText = """
            SELECT SEQUENCE_NAME, DATA_TYPE, START_VALUE, INCREMENT, MINIMUM_VALUE, MAXIMUM_VALUE, CYCLE_OPTION
            FROM INFORMATION_SCHEMA.SEQUENCES
            WHERE SEQUENCE_SCHEMA = $p_schema
            ORDER BY SEQUENCE_NAME
            """;

        command.Parameters.Add(Parameter(command, "p_schema", schema));

        var sequences = new List<H2SequenceRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sequences.Add(
                new H2SequenceRow(
                    reader.GetString(0),
                    Text(reader, 1),
                    Long(reader, 2),
                    reader.IsDBNull(3) ? null : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                    Long(reader, 4),
                    Long(reader, 5),
                    Text(reader, 6) == "YES"));
        }

        return sequences;
    }

    public IReadOnlyList<H2EnumValueRow> GetEnums(DbConnection connection, string schema)
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

        var values = new List<H2EnumValueRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            values.Add(new H2EnumValueRow(reader.GetString(0), reader.GetString(1)));
        }

        return values;
    }

    public IReadOnlyList<H2ForeignKeyRow> GetForeignKeys(DbConnection connection)
    {
        using var command = connection.CreateCommand();

        // REFERENTIAL_CONSTRAINTS names the referenced constraint, which is how the principal
        // table and its key columns are recovered: H2 exposes no "referenced column" of its own.
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

        var keys = new List<H2ForeignKeyRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            keys.Add(
                new H2ForeignKeyRow(
                    Text(reader, 0),
                    Text(reader, 1),
                    Text(reader, 2),
                    Text(reader, 3),
                    Text(reader, 4),
                    Text(reader, 5),
                    Text(reader, 6),
                    Text(reader, 7)));
        }

        return keys;
    }

    public string? GetPrincipalTableName(DbConnection connection, string? schema, string constraintName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
            WHERE CONSTRAINT_NAME = $p_name AND CONSTRAINT_SCHEMA = $p_schema
            """;

        command.Parameters.Add(Parameter(command, "p_name", constraintName));
        command.Parameters.Add(Parameter(command, "p_schema", (object?)schema ?? "PUBLIC"));

        return command.ExecuteScalar() as string;
    }

    public IReadOnlyList<string> GetPrincipalColumns(DbConnection connection, string? schema, string constraintName)
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

    public bool HasTables(DbConnection connection, string schema)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = $p";
        command.Parameters.Add(Parameter(command, "p", schema));

        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    public bool SchemaExists(DbConnection connection, string schema)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = $p";
        command.Parameters.Add(Parameter(command, "p", schema));

        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static string? Text(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? Int(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal)
            ? null
            : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static long? Long(DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal)
            ? null
            : Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static DbParameter Parameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        return parameter;
    }
}
