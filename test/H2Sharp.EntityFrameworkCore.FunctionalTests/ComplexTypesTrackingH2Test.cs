using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the fixture's model carries UNIQUE table constraints, and the port's
// CREATE TABLE reads only the primary key of them ("expected PRIMARY"). The reference accepts
// the statement the provider writes (checked on the jar).
[Trait("Category", "BlockedByPort")]
public class ComplexTypesTrackingH2Test : ComplexTypesTrackingTestBase<ComplexTypesTrackingH2Test.H2Fixture>
{
    public ComplexTypesTrackingH2Test(H2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        fixture.TestSqlLoggerFactory.Clear();
        fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class H2Fixture : FixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;
    }
}
