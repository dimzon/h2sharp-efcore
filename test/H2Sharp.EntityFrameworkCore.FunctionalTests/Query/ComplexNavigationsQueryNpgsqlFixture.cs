using Microsoft.EntityFrameworkCore.TestModels.ComplexNavigationsModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class ComplexNavigationsQueryNpgsqlFixture : ComplexNavigationsQueryRelationalFixtureBase
{
    protected override ITestStoreFactory TestStoreFactory
        => NpgsqlTestStoreFactory.Instance;

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        // We default to mapping DateTime to 'timestamp with time zone', but the seeding data has Unspecified DateTimes which aren't
        // supported.
        modelBuilder.Entity<Level1>().Property(l => l.Date).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Level2>().Property(l => l.Date).HasColumnType("timestamp without time zone");
    }
    // public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
    // {
    //     var optionsBuilder = base.AddOptions(builder);
    //     new H2DbContextOptionsBuilder(optionsBuilder).ReverseNullOrdering();
    //     return optionsBuilder;
    // }
}
