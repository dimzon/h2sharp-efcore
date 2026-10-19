namespace H2Sharp.EntityFrameworkCore.Query;

public class CompositeKeysSplitQueryNpgsqlTest : CompositeKeysSplitQueryRelationalTestBase<CompositeKeysQueryNpgsqlFixture>
{
    public CompositeKeysSplitQueryNpgsqlTest(
        CompositeKeysQueryNpgsqlFixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
