namespace H2Sharp.EntityFrameworkCore.Query;

public class TPCManyToManyNoTrackingQueryH2Test : TPCManyToManyNoTrackingQueryRelationalTestBase<TPCManyToManyQueryH2Fixture>
{
    public TPCManyToManyNoTrackingQueryH2Test(TPCManyToManyQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
