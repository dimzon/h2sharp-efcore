using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindKeylessEntitiesQueryH2Test : NorthwindKeylessEntitiesQueryRelationalTestBase<
    NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // The fixture's Northwind store has no CustomerQueryWithQueryFilter object for this query.
    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    [Trait("Category", "BlockedByPort")]
    public override Task KeylessEntity_with_nav_defining_query(bool async)
        => base.KeylessEntity_with_nav_defining_query(async);

    // ReSharper disable once UnusedParameter.Local
    public NorthwindKeylessEntitiesQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }


    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
