using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class TPTFiltersInheritanceQuerySqlServerTest : TPTFiltersInheritanceQueryTestBase<TPTFiltersInheritanceQuerySqlServerFixture>
{
    // ReSharper disable once UnusedParameter.Local
    public TPTFiltersInheritanceQuerySqlServerTest(
        TPTFiltersInheritanceQuerySqlServerFixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
