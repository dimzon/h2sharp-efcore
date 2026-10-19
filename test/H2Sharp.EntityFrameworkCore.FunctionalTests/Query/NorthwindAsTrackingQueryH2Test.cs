namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindAsTrackingQueryH2Test : NorthwindAsTrackingQueryTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindAsTrackingQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
    }
}
