using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class TPCInheritanceQueryH2Fixture : TPCInheritanceQueryFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
