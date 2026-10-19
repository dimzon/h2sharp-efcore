using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
        // H2Connection takes an H2 URL, not an ADO.NET connection string. "Data Source=mem:x" is the
        // spelling the rest of .NET uses and the one an application will hand to UseH2, so it is
        // translated here; passing it through unchanged makes the engine's URL parser fail on the
        // "Data Source=" prefix rather than on anything the caller wrote wrong.
        => new H2Connection(H2Url(ConnectionString));

    /// <summary>
    ///     The H2 URL for an ADO.NET connection string.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         H2Connection takes an H2 URL, not an ADO.NET connection string. "Data Source=mem:x" is the
    ///         spelling the rest of .NET uses and the one an application hands to UseH2, so it is
    ///         translated here; passing it through unchanged makes the engine's URL parser fail on the
    ///         "Data Source=" prefix rather than on anything the caller wrote wrong.
    ///     </para>
    ///     <para>
    ///         An H2 URL also carries settings -- MODE=PostgreSQL, DATABASE_TO_LOWER, DB_CLOSE_DELAY and
    ///         so on. Those arrive as further keys of the connection string, so the translation cannot
    ///         only swap the prefix: whatever is not the database name is put back as the ";Key=Value"
    ///         tail H2 expects on a URL, which is where those settings belong.
    ///     </para>
    /// </remarks>
    public static string H2Url(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        var builder = new H2ConnectionStringBuilder(connectionString);
        var url = builder.ToH2ConnectionString();

        var settings = new List<string>();
        foreach (string key in builder.Keys)
        {
            if (!string.Equals(key, "Data Source", StringComparison.OrdinalIgnoreCase))
            {
                settings.Add($"{key}={builder[key]}");
            }
        }

        return settings.Count == 0 ? url : url + ";" + string.Join(";", settings);
    }

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
