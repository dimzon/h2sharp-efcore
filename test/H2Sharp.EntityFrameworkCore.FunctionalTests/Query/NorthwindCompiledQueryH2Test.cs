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

    // The ADO parameter rewriter binds $args[1] as the scalar element of the argument array,
    // which is the layout EF compiled queries expect. Verified against H2 2.5: raw ?1[1]
    // is rejected by the engine too, so this is a .NET parameter-binding fix, not SQL syntax.
    public override void Query_with_array_parameter()
        => base.Query_with_array_parameter();

    public override Task Query_with_array_parameter_async()
        => base.Query_with_array_parameter_async();
}
