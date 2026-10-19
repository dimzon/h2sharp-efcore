using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class CompositeKeyEndToEndH2Test(CompositeKeyEndToEndH2Test.CompositeKeyEndToEndH2Fixture fixture)
    : CompositeKeyEndToEndTestBase<CompositeKeyEndToEndH2Test.CompositeKeyEndToEndH2Fixture>(fixture)
{
    public class CompositeKeyEndToEndH2Fixture : CompositeKeyEndToEndFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
