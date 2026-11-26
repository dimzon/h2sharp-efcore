using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class ComplexNavigationsCollectionsSplitQueryH2Test : ComplexNavigationsCollectionsSplitQueryRelationalTestBase<
    ComplexNavigationsQueryH2Fixture>
{
    public ComplexNavigationsCollectionsSplitQueryH2Test(
        ComplexNavigationsQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
