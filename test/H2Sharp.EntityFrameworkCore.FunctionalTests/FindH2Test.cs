using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: every save goes through SaveChanges and the port stops the update path with
// "Not ported yet: Prepared.update". All 411 tests of the three derived classes fail there, so the
// trait sits on this abstract class rather than on the methods.
[Trait("Category", "BlockedByPort")]
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
