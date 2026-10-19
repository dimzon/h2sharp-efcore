namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindIncludeQueryH2Test : NorthwindIncludeQueryRelationalTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindIncludeQueryH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
