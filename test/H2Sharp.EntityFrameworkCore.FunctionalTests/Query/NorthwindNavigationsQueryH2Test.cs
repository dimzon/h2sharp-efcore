namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindNavigationsQueryH2Test : NorthwindNavigationsQueryRelationalTestBase<
    NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindNavigationsQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
