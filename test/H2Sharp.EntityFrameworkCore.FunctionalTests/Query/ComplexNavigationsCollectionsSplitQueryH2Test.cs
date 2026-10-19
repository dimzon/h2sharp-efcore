namespace H2Sharp.EntityFrameworkCore.Query;

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
