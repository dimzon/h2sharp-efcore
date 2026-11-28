using Microsoft.EntityFrameworkCore.BulkUpdates;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPCInheritanceBulkUpdatesH2Fixture : TPCInheritanceBulkUpdatesFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;

    protected override string StoreName
        => "TPCBulkUpdates";

    public override bool UseGeneratedKeys
        => false;
}
