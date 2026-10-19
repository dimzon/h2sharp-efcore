using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class SeedingH2Test : SeedingTestBase
{
    protected override TestStore TestStore
        => H2TestStore.Create("SeedingTest");

    protected override SeedingContext CreateContextWithEmptyDatabase(string testId)
        => new SeedingH2Context(testId);

    protected class SeedingH2Context(string testId) : SeedingContext(testId)
    {
    }
}
