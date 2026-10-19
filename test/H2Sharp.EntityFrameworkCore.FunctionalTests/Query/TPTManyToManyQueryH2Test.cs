namespace H2Sharp.EntityFrameworkCore.Query;

public class TPTManyToManyQueryH2Test : TPTManyToManyQueryRelationalTestBase<TPTManyToManyQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public TPTManyToManyQueryH2Test(TPTManyToManyQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // TODO: #1232
    // protected override bool CanExecuteQueryString => true;
}
