using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     NodaTime specific extension methods for <see cref="H2DbContextOptionsBuilder" />.
/// </summary>
public static class NpgsqlNodaTimeDbContextOptionsBuilderExtensions
{
    /// <summary>
    ///     Configure NodaTime type mappings for Entity Framework.
    /// </summary>
    /// <returns> The options builder so that further configuration can be chained. </returns>
    public static H2DbContextOptionsBuilder UseNodaTime(
        this H2DbContextOptionsBuilder optionsBuilder)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));

        var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;

        var extension = coreOptionsBuilder.Options.FindExtension<NpgsqlNodaTimeOptionsExtension>()
            ?? new NpgsqlNodaTimeOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }
}
