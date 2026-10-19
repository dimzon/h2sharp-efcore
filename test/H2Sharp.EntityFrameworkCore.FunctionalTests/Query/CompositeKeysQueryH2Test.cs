namespace H2Sharp.EntityFrameworkCore.Query;

public class CompositeKeysQueryH2Test : CompositeKeysQueryRelationalTestBase<CompositeKeysQueryH2Fixture>
{
    public CompositeKeysQueryH2Test(
        CompositeKeysQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
