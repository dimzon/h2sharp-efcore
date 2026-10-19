namespace H2Sharp.EntityFrameworkCore.Update.Internal;

/// <summary>
///     The batch that executes a set of modification commands and counts the rows each one touched.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this was Npgsql's own batch, which read the affected row count out of the wire
///         protocol: PostgreSQL has no way of selecting the modified row count, because the SQL
///         standard's GET DIAGNOSTICS is only available inside PL/pgSQL.
///     </para>
///     <para>
///         H2Sharp's ADO layer reports the affected row count the ordinary way, so the batch is
///         EF Core's own affected-count batch. Nothing about it is H2-specific, and the H2-specific
///         part -- the connection, the type mappings, the SQL -- is already in place.
///     </para>
/// </remarks>
public class H2ModificationCommandBatch : AffectedCountModificationCommandBatch
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2ModificationCommandBatch(
        ModificationCommandBatchFactoryDependencies dependencies,
        int maxBatchSize)
        : base(dependencies)
        => MaxBatchSize = maxBatchSize;

    /// <summary>
    ///     The maximum number of <see cref="ModificationCommand" /> instances that can be added to a single batch.
    /// </summary>
    protected override int MaxBatchSize { get; }
}
