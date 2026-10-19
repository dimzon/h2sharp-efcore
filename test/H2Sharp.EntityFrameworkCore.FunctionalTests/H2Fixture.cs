using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class H2Fixture : ServiceProviderFixtureBase
{
    public TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();

    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
