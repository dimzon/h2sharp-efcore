using H2Sharp.EntityFrameworkCore.Infrastructure;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public static class H2DbContextOptionsBuilderExtensions
{
    public static H2DbContextOptionsBuilder ApplyConfiguration(this H2DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);

        optionsBuilder.CommandTimeout(H2TestStore.CommandTimeout);

        return optionsBuilder;
    }
}
