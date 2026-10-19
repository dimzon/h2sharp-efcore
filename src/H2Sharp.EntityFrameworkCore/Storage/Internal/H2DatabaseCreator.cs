using System.Data.Common;
using H2Sharp.Data;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this creator manages a PostgreSQL <em>database</em>: it opens an admin connection to
///         the maintenance database <c>postgres</c>, issues <c>CREATE DATABASE</c>, reloads type mappings
///         and clears the connection pool. None of that exists here. An H2 database is embedded in the
///         process, is created by opening it, and has no maintenance instance, no pool to clear and no
///         type mappings to reload.
///     </para>
///     <para>
///         The nearest thing H2 has to a PostgreSQL database is a <em>schema</em> inside a database, and
///         this creator manages that instead: <see cref="Exists" /> reports whether the schema is
///         present, <see cref="Create" /> issues <c>CREATE SCHEMA</c> and <see cref="Delete" /> issues
///         <c>DROP ALL OBJECTS</c> followed by <c>DROP SCHEMA</c>.
///     </para>
/// </remarks>
public class H2DatabaseCreator : RelationalDatabaseCreator
{
    private readonly IH2RelationalConnection _connection;
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2DatabaseCreator(
        RelationalDatabaseCreatorDependencies dependencies,
        IH2RelationalConnection connection,
        ISqlGenerationHelper sqlGenerationHelper)
        : base(dependencies)
    {
        _connection = connection;
        _sqlGenerationHelper = sqlGenerationHelper;
    }

    /// <summary>
    ///     The schema this creator manages, or <see langword="null" /> for the engine default.
    /// </summary>
    /// <remarks>
    ///     An H2 connection opens a database, and its objects live in a schema inside it. EF's default
    ///     schema is <c>public</c>, which H2 creates for you, so a caller that never set a schema gets
    ///     <see langword="null" /> and nothing is created or dropped.
    /// </remarks>
    private string? Schema
        => Dependencies.CurrentContext.Context.Model?.GetDefaultSchema() is { } schema && !string.Equals(schema, "public", StringComparison.Ordinal)
            ? schema
            : null;

    /// <inheritdoc />
    public override bool Exists()
        => Schema is not { } schema || SchemaExists(schema);

    /// <inheritdoc />
    public override void Create()
    {
        if (Schema is not { } schema)
        {
            // H2 creates the database and its PUBLIC schema when the connection opens, so there is
            // genuinely nothing to do rather than something this class has failed to implement.
            return;
        }

        Execute($"CREATE SCHEMA {_sqlGenerationHelper.DelimitIdentifier(schema)}");
        Exists();
    }

    /// <inheritdoc />
    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Exists());

    /// <inheritdoc />
    public override Task CreateAsync(CancellationToken cancellationToken = default)
    {
        Create();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override void Delete()
    {
        if (Schema is not { } schema)
        {
            throw new InvalidOperationException(
                "Dropping the engine default schema is not supported: H2 has no way to remove PUBLIC, " +
                "and dropping it would take the session's own unqualified object namespace with it. " +
                "Set a schema on the model if you want this creator to manage its lifetime.");
        }

        // DROP ALL OBJECTS removes the schema's contents but leaves the schema itself; the DROP does that.
        Execute($"DROP ALL OBJECTS {_sqlGenerationHelper.DelimitIdentifier(schema)}");
        Execute($"DROP SCHEMA {_sqlGenerationHelper.DelimitIdentifier(schema)} CASCADE");
    }

    /// <inheritdoc />
    public override Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        Delete();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override bool HasTables()
    {
        if (Schema is { } schema)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @p";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "p";
            parameter.Value = schema;
            command.Parameters.Add(parameter);

            return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
        }

        return false;
    }

    /// <inheritdoc />
    public override Task<bool> HasTablesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(HasTables());

    private bool SchemaExists(string schema)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @p";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "p";
        parameter.Value = schema;
        command.Parameters.Add(parameter);

        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private void Execute(string sql)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private H2Connection OpenConnection()
    {
        // Opening is the creation: H2Sharp creates the database file or registers the in-memory
        // database on first open, so there is no separate "connect to a maintenance instance" step
        // the way PostgreSQL needs one before CREATE DATABASE can be issued.
        //
        // A fresh connection is made from the connection string rather than reusing the one EF holds.
        // This class issues DDL against the catalog and the running DbConnection may be mid-reader.
        var connectionString = _connection.ConnectionString
            ?? throw new InvalidOperationException(
                $"{nameof(H2DatabaseCreator)} needs a connection string: an H2 database is addressed by " +
                "URL, and a context configured with a bare DbConnection instance cannot be duplicated here.");

        var connection = new H2Connection(connectionString);
        connection.Open();
        return connection;
    }
}
