using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the fixture's model carries UNIQUE table constraints, and the port's
// CREATE TABLE reads only the primary key of them ("expected PRIMARY"). The reference accepts
// the statement the provider writes (checked on the jar).
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
