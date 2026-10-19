using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class SeedingNpgsqlTest : SeedingTestBase
{
    protected override TestStore TestStore
        => NpgsqlTestStore.Create("SeedingTest");

    protected override SeedingContext CreateContextWithEmptyDatabase(string testId)
        => new SeedingNpgsqlContext(testId);

    protected class SeedingNpgsqlContext(string testId) : SeedingContext(testId)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2(NpgsqlTestStore.CreateConnectionString($"Seeds{TestId}"));
    }
}
