namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindCompiledQueryH2Test : NorthwindCompiledQueryTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindCompiledQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }
}
