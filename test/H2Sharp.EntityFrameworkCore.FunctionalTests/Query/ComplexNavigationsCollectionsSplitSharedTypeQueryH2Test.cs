namespace H2Sharp.EntityFrameworkCore.Query;

public class ComplexNavigationsCollectionsSplitSharedTypeQueryH2Test :
    ComplexNavigationsCollectionsSplitSharedTypeQueryRelationalTestBase<
        ComplexNavigationsSharedTypeQueryH2Fixture>
{
    public ComplexNavigationsCollectionsSplitSharedTypeQueryH2Test(
        ComplexNavigationsSharedTypeQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
