using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: every save goes through SaveChanges and the port stops the update path with
// "Not ported yet: Prepared.update". All 3133 tests fail there, so the trait sits on the class
// rather than on the methods.
[Trait("Category", "BlockedByPort")]
// ReSharper disable once UnusedMember.Global
public class LoadH2Test : LoadTestBase<LoadH2Test.LoadH2Fixture>
{
    public LoadH2Test(LoadH2Fixture fixture)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();

    protected override void RecordLog()
        => Sql = Fixture.TestSqlLoggerFactory.Sql;

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    private string Sql { get; set; }

    public class LoadH2Fixture : LoadFixtureBase
    {
        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
