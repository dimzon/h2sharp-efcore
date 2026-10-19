namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindAsNoTrackingQueryH2Test : NorthwindAsNoTrackingQueryTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindAsNoTrackingQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
    }
}
