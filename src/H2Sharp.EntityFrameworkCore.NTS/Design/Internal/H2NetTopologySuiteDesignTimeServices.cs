using Microsoft.Extensions.DependencyInjection.Extensions;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.NetTopologySuite.Scaffolding.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Design.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2NetTopologySuiteDesignTimeServices : IDesignTimeServices
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
        => serviceCollection
            .AddSingleton<IRelationalTypeMappingSourcePlugin, H2NetTopologySuiteTypeMappingSourcePlugin>()
            .AddSingleton<IProviderCodeGeneratorPlugin, H2NetTopologySuiteCodeGeneratorPlugin>()
            .TryAddSingleton<IH2NetTopologySuiteSingletonOptions, H2NetTopologySuiteSingletonOptions>();
}
