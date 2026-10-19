using Microsoft.EntityFrameworkCore.BulkUpdates;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPCInheritanceBulkUpdatesNpgsqlFixture : TPCInheritanceBulkUpdatesFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => NpgsqlTestStoreFactory.Instance;

    public override bool UseGeneratedKeys
        => false;
}
