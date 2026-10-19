using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class EntitySplittingNpgsqlTest(ITestOutputHelper testOutputHelper) : EntitySplittingTestBase(testOutputHelper)
{
    protected override ITestStoreFactory TestStoreFactory
        => NpgsqlTestStoreFactory.Instance;
}
