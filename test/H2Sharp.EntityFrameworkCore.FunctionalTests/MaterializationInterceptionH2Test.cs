#nullable enable

using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class MaterializationInterceptionH2Test :
    MaterializationInterceptionTestBase<MaterializationInterceptionH2Test.H2LibraryContext>
{
    public class H2LibraryContext(DbContextOptions options) : LibraryContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestEntity30244>().OwnsMany(e => e.Settings);

            // #2548
            // modelBuilder.Entity<TestEntity30244>().OwnsMany(e => e.Settings, b => b.ToJson());
        }
    }

    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
