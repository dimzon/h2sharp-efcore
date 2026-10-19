namespace H2Sharp.EntityFrameworkCore.Query;

internal class ManyToManyNoTrackingQueryH2Test
    : ManyToManyNoTrackingQueryRelationalTestBase<ManyToManyQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public ManyToManyNoTrackingQueryH2Test(ManyToManyQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // TODO: #1232
    // protected override bool CanExecuteQueryString => true;
}
