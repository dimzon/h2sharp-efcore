using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class OverzealousInitializationH2Test(OverzealousInitializationH2Test.OverzealousInitializationH2Fixture fixture)
    : OverzealousInitializationTestBase<OverzealousInitializationH2Test.OverzealousInitializationH2Fixture>(fixture)
{
    public class OverzealousInitializationH2Fixture : OverzealousInitializationFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
