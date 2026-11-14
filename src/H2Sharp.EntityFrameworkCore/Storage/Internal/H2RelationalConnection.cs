using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Storage;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
///
/// <remarks>
///     <para>
///         An H2 database is embedded in the process and is owned by an <see cref="H2DataSource" />: the
///         data source creates it, keeps it alive and hands out connections to it. Every connection of
///         this store therefore comes from a data source, and two contexts with the same connection string
///         share one database.
///     </para>
/// </remarks>
public class H2RelationalConnection : RelationalConnection, IH2RelationalConnection
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2RelationalConnection(RelationalConnectionDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     The data source that owns the database this store talks to.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A caller that supplied a data source owns the database; a caller that supplied a connection
    ///         string gets the data source of that connection string, and that database lives as long as
    ///         the process does. A caller that supplied a single connection has no data source to ask:
    ///         the connection is the only way to that database.
    ///     </para>
    /// </remarks>
    public virtual DbDataSource? DataSource
        => ResolveDataSource();

    private DbDataSource? ResolveDataSource()
    {
        var options = Dependencies.ContextOptions.FindExtension<H2OptionsExtension>();

        if (options?.DataSource is { } supplied)
        {
            return supplied;
        }

        if (options?.Connection is not null)
        {
            // The caller gave a connection and owns the database behind it. Its connection string
            // names that database, but a data source built from the string would open a database of
            // its own: the connection is the only way to the one the caller means.
            return null;
        }

        var connectionString = ConnectionString;
        if (string.IsNullOrEmpty(connectionString))
        {
            return null;
        }

        return H2DataSources.GetOrCreate(connectionString);
    }

    /// <summary>
    ///     Indicates whether the store connection supports ambient transactions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         True. The engine's connection enlists itself in an ambient
    ///         <see cref="System.Transactions.Transaction" /> when one is in scope, and EF Core then leaves
    ///         the transaction to that ambient one instead of starting a transaction of its own.
    ///     </para>
    /// </remarks>
    protected override bool SupportsAmbientTransactions
        => true;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override DbConnection CreateDbConnection()
        => DataSource?.CreateConnection()
           ?? throw new InvalidOperationException(
               "An H2 context needs a connection string or a data source: the database is owned by a data source.");

    /// <summary>
    ///     The connection string of the store.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A connection that comes from a data source belongs to the database of that data
    ///         source, and the engine does not let the connection string of such a connection
    ///         change under it. The connection is therefore given up when the string changes, and
    ///         the next use takes a connection from the data source of the new string.
    ///     </para>
    ///     <para>
    ///         A connection the caller gave is the caller's own: its connection string is set the
    ///         way the engine allows, and the caller keeps the connection.
    ///     </para>
    /// </remarks>
    public override string? ConnectionString
    {
        get => base.ConnectionString;
        set
        {
            if (CallerSuppliedConnection)
            {
                base.ConnectionString = value;
                return;
            }

            SetDbConnection(null, contextOwnsConnection: false);
            base.ConnectionString = value;
        }
    }

    /// <summary>
    ///     Whether the caller gave a connection of its own, which the provider must not replace.
    /// </summary>
    private bool CallerSuppliedConnection
        => Dependencies.ContextOptions.FindExtension<H2OptionsExtension>()?.Connection is not null;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [AllowNull]
    public new virtual H2Connection DbConnection
    {
        get => (H2Connection)base.DbConnection;
        set => base.DbConnection = value;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IH2RelationalConnection CloneWith(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(
            ((H2OptionsExtension)RelationalOptionsExtension.Extract(Dependencies.ContextOptions))
                .WithConnectionString(connectionString));

        return new H2RelationalConnection(Dependencies with { ContextOptions = optionsBuilder.Options });
    }
}
