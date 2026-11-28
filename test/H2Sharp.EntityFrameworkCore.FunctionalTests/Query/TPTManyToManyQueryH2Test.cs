using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class TPTManyToManyQueryH2Test : TPTManyToManyQueryRelationalTestBase<TPTManyToManyQueryH2Fixture>
{
    // H2 2.5 has no LATERAL for these ordered skip-navigation shapes.
    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    [Trait("Category", "BlockedByPort")]
    public override Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(bool async)
        => base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where(async);

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    [Trait("Category", "BlockedByPort")]
    public override Task Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(bool async)
        => base.Filtered_include_skip_navigation_order_by_skip_take_then_include_skip_navigation_where_EF_Property(async);

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    [Trait("Category", "BlockedByPort")]
    public override Task Skip_navigation_order_by_single_or_default(bool async)
        => base.Skip_navigation_order_by_single_or_default(async);

    // ReSharper disable once UnusedParameter.Local
    public TPTManyToManyQueryH2Test(TPTManyToManyQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // TODO: #1232
    // protected override bool CanExecuteQueryString => true;
}
