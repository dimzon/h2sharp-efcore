using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class SharedTypeQueryH2Test : SharedTypeQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;

    public override Task Can_use_shared_type_entity_type_in_query_filter_with_from_sql(bool async)
        => Task.CompletedTask; // https://github.com/dotnet/efcore/issues/25661
}
