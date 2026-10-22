using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     H2Sharp.EntityFrameworkCore.NetTopologySuite extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class H2NetTopologySuiteServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the services required for NetTopologySuite support in the H2 provider for Entity Framework.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddEntityFrameworkH2NetTopologySuite(
        this IServiceCollection serviceCollection)
    {
        Check.NotNull(serviceCollection, nameof(serviceCollection));

        new EntityFrameworkH2ServicesBuilder(serviceCollection)
            .TryAdd<ISingletonOptions, IH2NetTopologySuiteSingletonOptions>(p => p.GetRequiredService<IH2NetTopologySuiteSingletonOptions>())
            .TryAdd<IRelationalTypeMappingSourcePlugin, H2NetTopologySuiteTypeMappingSourcePlugin>()
            .TryAdd<IAggregateMethodCallTranslatorPlugin, H2NetTopologySuiteAggregateMethodCallTranslatorPlugin>()
            .TryAdd<IConventionSetPlugin, H2NetTopologySuiteConventionSetPlugin>()
            .TryAddProviderSpecificServices(
                x => x.TryAddSingleton<IH2NetTopologySuiteSingletonOptions, H2NetTopologySuiteSingletonOptions>());

        return serviceCollection;
    }
}
