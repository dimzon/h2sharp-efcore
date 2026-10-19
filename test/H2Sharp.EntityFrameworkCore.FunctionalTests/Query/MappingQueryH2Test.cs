using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

// ReSharper disable once UnusedMember.Global
public class MappingQueryH2Test : MappingQueryTestBase<MappingQueryH2Test.MappingQueryH2Fixture>
{
    public MappingQueryH2Test(MappingQueryH2Fixture fixture)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public class MappingQueryH2Fixture : MappingQueryFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2NorthwindTestStoreFactory.Instance;

        protected override string DatabaseSchema { get; } = "public";

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<MappedCustomer>(
                e =>
                {
                    e.Property(c => c.CompanyName2).Metadata.SetColumnName("CompanyName");
                    e.Metadata.SetTableName("Customers");
                    e.Metadata.SetSchema("public");
                });

            modelBuilder.Entity<MappedEmployee>()
                .Property(c => c.EmployeeID)
                .HasColumnType("int");
        }
    }
}
