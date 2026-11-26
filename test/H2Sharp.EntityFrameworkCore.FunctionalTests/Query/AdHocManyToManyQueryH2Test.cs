using H2Sharp.EntityFrameworkCore.TestUtilities;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class AdHocManyToManyQueryH2Test : AdHocManyToManyQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
