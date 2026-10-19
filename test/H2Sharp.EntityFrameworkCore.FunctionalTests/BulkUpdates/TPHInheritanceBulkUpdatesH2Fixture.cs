using Microsoft.EntityFrameworkCore.BulkUpdates;
using Microsoft.EntityFrameworkCore.TestModels.InheritanceModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPHInheritanceBulkUpdatesH2Fixture : TPHInheritanceBulkUpdatesFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        modelBuilder.Entity<AnimalQuery>().HasNoKey().ToSqlQuery("""
            SELECT * FROM "Animals"
            """);
    }
}
