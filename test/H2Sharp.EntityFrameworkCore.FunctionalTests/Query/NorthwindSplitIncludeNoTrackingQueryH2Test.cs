namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindSplitIncludeNoTrackingQueryH2Test : NorthwindSplitIncludeNoTrackingQueryTestBase<
    NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindSplitIncludeNoTrackingQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        // TestSqlLoggerFactory.CaptureOutput(testOutputHelper);
    }
}
