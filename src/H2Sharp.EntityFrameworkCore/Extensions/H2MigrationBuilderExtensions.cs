// ReSharper disable once CheckNamespace

namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public static class H2MigrationBuilderExtensions
{
    /// <summary>
    ///     Returns true if the active provider in a migration is the H2 provider.
    /// </summary>
    /// The migrationBuilder from the parameters on
    /// <see cref="Migration.Up(MigrationBuilder)" />
    /// or
    /// <see cref="Migration.Down(MigrationBuilder)" />
    /// .
    /// <returns>True if H2 is being used; false otherwise.</returns>
    public static bool IsH2(this MigrationBuilder builder)
        => builder.ActiveProvider == typeof(H2MigrationBuilderExtensions).GetTypeInfo().Assembly.GetName().Name;
}
