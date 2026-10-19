namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindQueryTaggingQueryH2Test : NorthwindQueryTaggingQueryTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindQueryTaggingQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
