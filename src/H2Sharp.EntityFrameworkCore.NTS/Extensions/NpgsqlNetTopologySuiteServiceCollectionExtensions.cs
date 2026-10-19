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
public static class NpgsqlNetTopologySuiteServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the services required for NetTopologySuite support in the Npgsql provider for Entity Framework.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddEntityFrameworkNpgsqlNetTopologySuite(
        this IServiceCollection serviceCollection)
    {
        Check.NotNull(serviceCollection, nameof(serviceCollection));

        new EntityFrameworkNpgsqlServicesBuilder(serviceCollection)
            .TryAdd<INpgsqlDataSourceConfigurationPlugin, NetTopologySuiteDataSourceConfigurationPlugin>()
            .TryAdd<ISingletonOptions, INpgsqlNetTopologySuiteSingletonOptions>(p => p.GetRequiredService<INpgsqlNetTopologySuiteSingletonOptions>())
            .TryAdd<IRelationalTypeMappingSourcePlugin, NpgsqlNetTopologySuiteTypeMappingSourcePlugin>()
            .TryAdd<IMethodCallTranslatorPlugin, NpgsqlNetTopologySuiteMethodCallTranslatorPlugin>()
            .TryAdd<IAggregateMethodCallTranslatorPlugin, NpgsqlNetTopologySuiteAggregateMethodCallTranslatorPlugin>()
            .TryAdd<IMemberTranslatorPlugin, NpgsqlNetTopologySuiteMemberTranslatorPlugin>()
            .TryAdd<IConventionSetPlugin, NpgsqlNetTopologySuiteConventionSetPlugin>()
            .TryAddProviderSpecificServices(
                x => x.TryAddSingleton<INpgsqlNetTopologySuiteSingletonOptions, NpgsqlNetTopologySuiteSingletonOptions>());

        return serviceCollection;
    }
}
