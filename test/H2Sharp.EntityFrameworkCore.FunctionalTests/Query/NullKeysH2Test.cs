using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class NullKeysH2Test(NullKeysH2Test.NullKeysH2Fixture fixture)
    : NullKeysTestBase<NullKeysH2Test.NullKeysH2Fixture>(fixture)
{
    public class NullKeysH2Fixture : NullKeysFixtureBase
    {
        protected override string StoreName { get; } = "StringsContext";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
