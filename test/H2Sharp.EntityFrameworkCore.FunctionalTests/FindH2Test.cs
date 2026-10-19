using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class FindH2Test : FindTestBase<FindH2Test.FindH2Fixture>
{
    protected FindH2Test(FindH2Fixture fixture)
        : base(fixture)
    {
        fixture.TestSqlLoggerFactory.Clear();
    }

    public class FindH2TestSet(FindH2Fixture fixture) : FindH2Test(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaSetFinder();
    }

    public class FindH2TestContext(FindH2Fixture fixture) : FindH2Test(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaContextFinder();
    }

    public class FindH2TestNonGeneric(FindH2Fixture fixture) : FindH2Test(fixture)
    {
        protected override TestFinder Finder { get; } = new FindViaNonGenericContextFinder();
    }

    public class FindH2Fixture : FindFixtureBase
    {
        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
