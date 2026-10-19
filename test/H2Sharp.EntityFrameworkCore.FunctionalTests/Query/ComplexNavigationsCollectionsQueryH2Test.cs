namespace H2Sharp.EntityFrameworkCore.Query;

public class ComplexNavigationsCollectionsQueryH2Test : ComplexNavigationsCollectionsQueryRelationalTestBase<
    ComplexNavigationsQueryH2Fixture>
{
    public ComplexNavigationsCollectionsQueryH2Test(
        ComplexNavigationsQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
