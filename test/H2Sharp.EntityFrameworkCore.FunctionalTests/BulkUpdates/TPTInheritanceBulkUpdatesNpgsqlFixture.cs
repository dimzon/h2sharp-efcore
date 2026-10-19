using Microsoft.EntityFrameworkCore.BulkUpdates;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPTInheritanceBulkUpdatesNpgsqlFixture : TPTInheritanceBulkUpdatesFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => NpgsqlTestStoreFactory.Instance;
}
