namespace H2Sharp.EntityFrameworkCore.Query;

public class ManyToManyQueryH2Test : ManyToManyQueryRelationalTestBase<ManyToManyQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public ManyToManyQueryH2Test(ManyToManyQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // H2 2.5 has no LATERAL, so the provider refuses to write the outer apply these need.
    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an outer apply cannot be written as SQL")]
    public override Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(bool async)
        => base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an outer apply cannot be written as SQL")]
    public override Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(bool async)
        => base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an outer apply cannot be written as SQL")]
    public override Task Skip_navigation_order_by_single_or_default(bool async)
        => base.Skip_navigation_order_by_single_or_default(async);
}
