using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.NodaTime.Query.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     H2Sharp.EntityFrameworkCore.NodaTime extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class H2NodaTimeServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the services required for NodaTime support in the H2 provider for Entity Framework.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddEntityFrameworkH2NodaTime(
        this IServiceCollection serviceCollection)
    {
        Check.NotNull(serviceCollection, nameof(serviceCollection));

        new EntityFrameworkH2ServicesBuilder(serviceCollection)
            .TryAdd<IH2DataSourceConfigurationPlugin, NodaTimeDataSourceConfigurationPlugin>()
            .TryAdd<IRelationalTypeMappingSourcePlugin, H2NodaTimeTypeMappingSourcePlugin>()
            .TryAdd<IMethodCallTranslatorPlugin, H2NodaTimeMethodCallTranslatorPlugin>()
            .TryAdd<IAggregateMethodCallTranslatorPlugin, H2NodaTimeAggregateMethodCallTranslatorPlugin>()
            .TryAdd<IMemberTranslatorPlugin, H2NodaTimeMemberTranslatorPlugin>()
            .TryAdd<IEvaluatableExpressionFilterPlugin, H2NodaTimeEvaluatableExpressionFilterPlugin>();

        return serviceCollection;
    }
}
