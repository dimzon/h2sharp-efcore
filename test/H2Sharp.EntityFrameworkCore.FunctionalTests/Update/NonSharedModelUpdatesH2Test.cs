using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Update;

public class NonSharedModelUpdatesH2Test : NonSharedModelUpdatesTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
