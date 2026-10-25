using H2Sharp.EntityFrameworkCore.NodaTime.Query.Internal;
using H2Sharp.EntityFrameworkCore.Storage;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     H2Sharp.EntityFrameworkCore.NodaTime extension methods for <see cref="IServiceCollection" />.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this plugin brought its own type mappings and relied on the Npgsql ADO driver to
///         convert the values. The H2Sharp ADO layer knows only the BCL types, so the conversion is
///         done here instead, by a convention that gives each NodaTime property a value converter.
///         The store types then come from the main provider's own mappings, which already know that
///         a DateOnly is a DATE and a DateTimeOffset a TIMESTAMP WITH TIME ZONE.
///     </para>
/// </remarks>
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

        // The plugin supplies the type mappings for the NodaTime types. Each mapping carries the
        // conversion to the BCL type the H2 engine understands, so nothing here depends on the ADO
        // driver having a handler for NodaTime -- which it does not.
        serviceCollection.AddScoped<IRelationalTypeMappingSourcePlugin, H2NodaTimeTypeMappingSourcePlugin>();

        // The part functions of H2 answer the date and time members of the types whose column
        // carries no time zone; the rest stays untranslated, and EF Core says so.
        serviceCollection.AddScoped<IMemberTranslatorPlugin, H2NodaTimeMemberTranslatorPlugin>(
            sp => new H2NodaTimeMemberTranslatorPlugin(
                [new H2NodaTimeMemberTranslator(sp.GetRequiredService<ISqlExpressionFactory>())]));

        return serviceCollection;
    }
}
