using H2Sharp.EntityFrameworkCore.TestUtilities;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class AdHocQueryFiltersQueryH2Test : AdHocQueryFiltersQueryRelationalTestBase
{
    // H2 cannot bind an outer column through a derived table used twice in the same projection.
    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    [Trait("Category", "BlockedByPort")]
    public override Task Group_by_multiple_aggregate_joining_different_tables(bool async)
        => base.Group_by_multiple_aggregate_joining_different_tables(async);

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    [Trait("Category", "BlockedByPort")]
    public override Task Group_by_multiple_aggregate_joining_different_tables_with_query_filter(bool async)
        => base.Group_by_multiple_aggregate_joining_different_tables_with_query_filter(async);

    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
