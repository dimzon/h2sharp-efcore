using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class EntitySplittingH2Test(ITestOutputHelper testOutputHelper) : EntitySplittingTestBase(testOutputHelper)
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
