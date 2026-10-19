using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class AdHocNavigationsQueryH2Test : AdHocNavigationsQueryRelationalTestBase
{
    // Cannot write DateTime with Kind=Local to PostgreSQL type 'timestamp with time zone', only UTC is supported.
    public override Task Reference_include_on_derived_type_with_sibling_works()
        => Assert.ThrowsAsync<DbUpdateException>(() => base.Reference_include_on_derived_type_with_sibling_works());

    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
