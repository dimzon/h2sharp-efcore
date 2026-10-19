using System.Data.Common;
using System.Transactions;
using H2Sharp.Data;

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
///         The upstream H2 provider builds its connections through <c>H2DataSource</c>, a
///         pooling and multiplexing layer that also carries the plugin, enum-mapping and TLS
///         configuration. H2Sharp has no equivalent: <see cref="H2Connection" /> opens the database
///         directly and there is no separate configuration surface to hang those features on. The
///         data source, its manager and the plugin interface are therefore gone rather than emulated.
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
    ///     Indicates whether the store connection supports ambient transactions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         False, unlike upstream. An H2 database is embedded in the process and its transaction is
    ///         owned by <see cref="H2Connection" />; there is no enlistment path from an ambient
    ///         <see cref="System.Transactions.Transaction" /> into the engine. Reporting true would let EF
    ///         Core suppress its own transaction when an ambient one happens to be in scope, and the work
    ///         would then run outside any transaction H2Sharp knows about.
    ///     </para>
    /// </remarks>
    protected override bool SupportsAmbientTransactions
        => false;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override DbConnection CreateDbConnection()
        => new H2Connection(ConnectionString);

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
    public override Transaction? CurrentAmbientTransaction
        => null;

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
            RelationalOptionsExtension.Extract(Dependencies.ContextOptions)
                .WithConnectionString(null)
                .WithConnection(new H2Connection(connectionString)));

        return new H2RelationalConnection(Dependencies with { ContextOptions = optionsBuilder.Options });
    }
}
