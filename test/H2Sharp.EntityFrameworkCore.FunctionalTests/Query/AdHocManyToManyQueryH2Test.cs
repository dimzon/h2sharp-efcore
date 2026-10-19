using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class AdHocManyToManyQueryH2Test : AdHocManyToManyQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
