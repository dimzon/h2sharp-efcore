using H2Sharp.EntityFrameworkCore.Design.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class DesignTimeH2Test(DesignTimeH2Test.DesignTimeH2Fixture fixture)
    : DesignTimeTestBase<DesignTimeH2Test.DesignTimeH2Fixture>(fixture)
{
    protected override Assembly ProviderAssembly
        => typeof(H2DesignTimeServices).Assembly;

    public class DesignTimeH2Fixture : DesignTimeFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
