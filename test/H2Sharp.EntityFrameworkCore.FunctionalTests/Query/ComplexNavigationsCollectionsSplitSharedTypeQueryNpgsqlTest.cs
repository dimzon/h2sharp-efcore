namespace H2Sharp.EntityFrameworkCore.Query;

public class ComplexNavigationsCollectionsSplitSharedTypeQueryNpgsqlTest :
    ComplexNavigationsCollectionsSplitSharedTypeQueryRelationalTestBase<
        ComplexNavigationsSharedTypeQueryNpgsqlFixture>
{
    public ComplexNavigationsCollectionsSplitSharedTypeQueryNpgsqlTest(
        ComplexNavigationsSharedTypeQueryNpgsqlFixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
