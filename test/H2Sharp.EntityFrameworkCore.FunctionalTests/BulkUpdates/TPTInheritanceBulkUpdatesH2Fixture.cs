using Microsoft.EntityFrameworkCore.BulkUpdates;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPTInheritanceBulkUpdatesH2Fixture : TPTInheritanceBulkUpdatesFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
