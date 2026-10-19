namespace H2Sharp.EntityFrameworkCore.Query;

public class ComplexNavigationsCollectionsSharedTypeQueryH2Test : ComplexNavigationsCollectionsSharedTypeQueryRelationalTestBase<
    ComplexNavigationsSharedTypeQueryH2Fixture>
{
    public ComplexNavigationsCollectionsSharedTypeQueryH2Test(
        ComplexNavigationsSharedTypeQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
