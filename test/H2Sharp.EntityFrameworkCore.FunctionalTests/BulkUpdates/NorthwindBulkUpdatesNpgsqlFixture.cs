using Microsoft.EntityFrameworkCore.BulkUpdates;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class NorthwindBulkUpdatesNpgsqlFixture<TModelCustomizer> : NorthwindBulkUpdatesRelationalFixture<TModelCustomizer>
    where TModelCustomizer : ITestModelCustomizer, new()
{
    protected override ITestStoreFactory TestStoreFactory
        => NpgsqlNorthwindTestStoreFactory.Instance;

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        modelBuilder.Entity<MostExpensiveProduct>()
            .Property(p => p.UnitPrice)
            .HasColumnType("money");
    }

    protected override Type ContextType
        => typeof(NorthwindNpgsqlContext);
}
