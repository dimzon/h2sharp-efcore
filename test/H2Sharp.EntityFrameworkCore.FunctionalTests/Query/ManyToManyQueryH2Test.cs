namespace H2Sharp.EntityFrameworkCore.Query;

internal class ManyToManyQueryH2Test : ManyToManyQueryRelationalTestBase<ManyToManyQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public ManyToManyQueryH2Test(ManyToManyQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
