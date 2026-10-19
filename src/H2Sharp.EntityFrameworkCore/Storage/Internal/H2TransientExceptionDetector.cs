using H2ErrorCode = H2Sharp.Api.ErrorCode;
using H2SharpException = H2Sharp.H2SharpException;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this filtered on an NpgsqlException flag set by the driver from the SQLSTATE class,
///         covering things like a server restart, a failover or a dropped socket. None of that reaches
///         an application here: the engine is in the process, so there is no network to lose.
///     </para>
///     <para>
///         What is left is contention -- H2 refuses a statement whose lock cannot be taken in time, and
///         the caller may legitimately retry that. Those are named by code rather than by a driver flag,
///         because H2SharpException carries the error code rather than a transience hint.
///     </para>
/// </remarks>
public static class H2TransientExceptionDetector
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static bool ShouldRetryOn(Exception? ex)
        => ex switch
        {
            // The row another transaction holds was not released before the lock timeout expired.
            // Retrying after a short delay is the normal answer.
            H2SharpException { H2ErrorCode: H2ErrorCode.LockTimeout1 } => true,

            // H2 aborts one of two transactions that each want what the other holds.
            H2SharpException { H2ErrorCode: H2ErrorCode.Deadlock1 } => true,

            // Same shape as a duplicate-key race on an identity value: retrying re-reads and proceeds.
            H2SharpException { H2ErrorCode: H2ErrorCode.ConcurrentUpdate1 } => true,

            TimeoutException => true,
            _ => false
        };
}
