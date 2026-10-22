using H2Sharp.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Extension methods for <see cref="AlterDatabaseOperation" /> for H2-specific metadata.
/// </summary>
public static class H2AlterDatabaseOperationExtensions
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static IReadOnlyList<PostgresEnum> GetPostgresEnums(this AlterDatabaseOperation operation)
        => PostgresEnum.GetPostgresEnums(operation).ToArray();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static IReadOnlyList<PostgresEnum> GetOldPostgresEnums(this AlterDatabaseOperation operation)
        => PostgresEnum.GetPostgresEnums(operation.OldDatabase).ToArray();
}
