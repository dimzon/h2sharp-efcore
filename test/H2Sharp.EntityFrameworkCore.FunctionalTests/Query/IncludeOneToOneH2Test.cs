using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

// ReSharper disable once UnusedMember.Global
public class IncludeOneToOneH2Test : IncludeOneToOneTestBase<IncludeOneToOneH2Test.OneToOneQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public IncludeOneToOneH2Test(OneToOneQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public class OneToOneQueryH2Fixture : OneToOneQueryFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();
    }
}
