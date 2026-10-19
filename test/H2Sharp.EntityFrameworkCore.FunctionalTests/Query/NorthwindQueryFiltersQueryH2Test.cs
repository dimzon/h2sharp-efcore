namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindQueryFiltersQueryH2Test : NorthwindQueryFiltersQueryTestBase<
    NorthwindQueryH2Fixture<NorthwindQueryFiltersCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindQueryFiltersQueryH2Test(
        NorthwindQueryH2Fixture<NorthwindQueryFiltersCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
