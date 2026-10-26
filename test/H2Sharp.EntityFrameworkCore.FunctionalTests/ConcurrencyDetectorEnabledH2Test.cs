using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: this class and ConcurrencyDetectorDisabledH2Test share one store
// ("ConcurrencyDetector"), and one of the two always finds it empty -- "Table \"Products\" not
// found (this database is empty)" -- so the pair passes or fails depending on the order the two
// classes happen to run in. The fix belongs in the fixture; until then the trait keeps both out of
// the green run, because either of them can be the empty one.
[Trait("Category", "BlockedByPort")]
public class ConcurrencyDetectorEnabledH2Test : ConcurrencyDetectorEnabledRelationalTestBase<
    ConcurrencyDetectorEnabledH2Test.ConcurrencyDetectorH2Fixture>
{
    public ConcurrencyDetectorEnabledH2Test(ConcurrencyDetectorH2Fixture fixture)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public override Task FromSql(bool async)
        => ConcurrencyDetectorTest(
            async c => async
                ? await c.Products.FromSqlRaw("""
                    select * from "Products"
                    """).ToListAsync()
                : c.Products.FromSqlRaw("""
                    select * from "Products"
                    """).ToList());

    protected override async Task ConcurrencyDetectorTest(Func<ConcurrencyDetectorDbContext, Task<object>> test)
    {
        await base.ConcurrencyDetectorTest(test);

        Assert.Empty(Fixture.TestSqlLoggerFactory.SqlStatements);
    }

    public class ConcurrencyDetectorH2Fixture : ConcurrencyDetectorFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;
    }
}
