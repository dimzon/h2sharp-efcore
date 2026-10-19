using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindQueryH2Fixture<TModelCustomizer> : NorthwindQueryRelationalFixture<TModelCustomizer>
    where TModelCustomizer : ITestModelCustomizer, new()
{
    protected override ITestStoreFactory TestStoreFactory
        => H2NorthwindTestStoreFactory.Instance;

    protected override Type ContextType
        => typeof(NorthwindH2Context);


    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        // Note that we map price properties to numeric(12,2) columns, not to money as in SqlServer, since in
        // PG, money is discouraged/obsolete and various tests fail with it.

        modelBuilder.Entity<Order>(
            b =>
            {
                b.Property(o => o.EmployeeID).HasColumnType("int");
                b.Property(o => o.OrderDate).HasColumnType("timestamp without time zone");
            });

        modelBuilder.Entity<Employee>(
            b =>
            {
                b.Property(c => c.EmployeeID).HasColumnType("int");
                b.Property(c => c.ReportsTo).HasColumnType("int");
            });

        modelBuilder.Entity<Order>()
            .Property(o => o.EmployeeID)
            .HasColumnType("int");

        modelBuilder.Entity<Product>()
            .Property(p => p.UnitsInStock)
            .HasColumnType("smallint");
    }
}
