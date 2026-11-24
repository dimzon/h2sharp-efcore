namespace H2Sharp.EntityFrameworkCore.Query;

[Collection("Northwind")]
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

    // Port defect: the parser reads "$1[1]" (compiled query array element) as a cast and fails
    // with "Unknown data type: (?1)".
    [Trait("Category", "BlockedByPort")]
    public override void Query_with_array_parameter()
        => base.Query_with_array_parameter();

    [Trait("Category", "BlockedByPort")]
    public override Task Query_with_array_parameter_async()
        => base.Query_with_array_parameter_async();
}
