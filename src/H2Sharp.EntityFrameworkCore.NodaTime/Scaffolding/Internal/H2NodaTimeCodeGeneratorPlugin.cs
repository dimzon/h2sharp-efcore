using H2Sharp.EntityFrameworkCore.Infrastructure;

namespace H2Sharp.EntityFrameworkCore.NodaTime.Scaffolding.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2NodaTimeCodeGeneratorPlugin : ProviderCodeGeneratorPlugin
{
    private static readonly MethodInfo _useNodaTimeMethodInfo
        = typeof(H2NodaTimeDbContextOptionsBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2NodaTimeDbContextOptionsBuilderExtensions.UseNodaTime),
            typeof(H2DbContextOptionsBuilder));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override MethodCallCodeFragment GenerateProviderOptions()
        => new(_useNodaTimeMethodInfo);
}
