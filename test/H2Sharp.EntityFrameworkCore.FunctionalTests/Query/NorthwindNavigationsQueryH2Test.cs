namespace H2Sharp.EntityFrameworkCore.Query;

[Collection("Northwind")]
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

    // H2 2.5 does not resolve a qualified outer column inside a DISTINCT derived table
    // nested in a scalar subquery ("Column c.Country not found"), verified on the reference jar.
    [ConditionalTheory(Skip = "H2 2.5 does not resolve the qualified outer column in this shape; verified on h2.jar")]
    public override Task Navigation_in_subquery_referencing_outer_query_with_client_side_result_operator_and_count(bool async)
        => base.Navigation_in_subquery_referencing_outer_query_with_client_side_result_operator_and_count(async);
}
