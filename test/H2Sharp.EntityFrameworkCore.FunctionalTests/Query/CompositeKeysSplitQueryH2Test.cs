namespace H2Sharp.EntityFrameworkCore.Query;

public class CompositeKeysSplitQueryH2Test : CompositeKeysSplitQueryRelationalTestBase<CompositeKeysQueryH2Fixture>
{
    public CompositeKeysSplitQueryH2Test(
        CompositeKeysQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
