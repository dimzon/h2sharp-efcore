using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the fixture's model carries UNIQUE and FOREIGN KEY table constraints, and the
// port's CREATE TABLE reads only the primary key of them ("expected PRIMARY"). The reference
// accepts the statement the provider writes (checked on the jar).
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
