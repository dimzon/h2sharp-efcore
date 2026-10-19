using Microsoft.EntityFrameworkCore.TestModels.ComplexNavigationsModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class ComplexNavigationsSharedTypeQueryH2Fixture : ComplexNavigationsSharedTypeQueryRelationalFixtureBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        // We default to mapping DateTime to 'timestamp with time zone', but the seeding data has Unspecified DateTimes which aren't
        // supported.
        modelBuilder.Entity<Level1>().Property(l => l.Date).HasColumnType("timestamp without time zone");
        modelBuilder.Entity("Level1.OneToOne_Required_PK1#Level2").Property("Date").HasColumnType("timestamp without time zone");
    }
}
