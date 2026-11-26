using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class TPCManyToManyQueryH2Test : TPCManyToManyQueryRelationalTestBase<TPCManyToManyQueryH2Fixture>
{
    public TPCManyToManyQueryH2Test(TPCManyToManyQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
