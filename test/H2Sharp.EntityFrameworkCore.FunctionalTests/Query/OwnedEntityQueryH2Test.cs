using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class OwnedEntityQueryH2Test : OwnedEntityQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
