using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the fixture's model carries UNIQUE and FOREIGN KEY table constraints, and the
// port's CREATE TABLE reads only the primary key of them ("expected PRIMARY"). The reference
// accepts the statement the provider writes (checked on the jar).
[Trait("Category", "BlockedByPort")]
public class StoreGeneratedFixupH2Test(StoreGeneratedFixupH2Test.StoreGeneratedFixupH2Fixture fixture)
    : StoreGeneratedFixupRelationalTestBase<StoreGeneratedFixupH2Test.StoreGeneratedFixupH2Fixture>(fixture)
{
    [Fact]
    public Task Temp_values_are_replaced_on_save()
        => ExecuteWithStrategyInTransactionAsync(
            async context =>
            {
                var entry = context.Add(new TestTemp());

                Assert.True(entry.Property(e => e.Id).IsTemporary);
                Assert.False(entry.Property(e => e.NotId).IsTemporary);

                var tempValue = entry.Property(e => e.Id).CurrentValue;

                await context.SaveChangesAsync();

                Assert.False(entry.Property(e => e.Id).IsTemporary);
                Assert.NotEqual(tempValue, entry.Property(e => e.Id).CurrentValue);
            });

    protected override void MarkIdsTemporary(DbContext context, object dependent, object principal)
    {
        var entry = context.Entry(dependent);
        entry.Property("Id1").IsTemporary = true;
        entry.Property("Id2").IsTemporary = true;

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsForeignKey())
            {
                property.IsTemporary = true;
            }
        }

        entry = context.Entry(principal);
        entry.Property("Id1").IsTemporary = true;
        entry.Property("Id2").IsTemporary = true;
    }

    protected override void MarkIdsTemporary(DbContext context, object game, object level, object item)
    {
        var entry = context.Entry(game);
        entry.Property("Id").IsTemporary = true;

        entry = context.Entry(item);
        entry.Property("Id").IsTemporary = true;
    }

    protected override bool EnforcesFKs
        => true;

    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class StoreGeneratedFixupH2Fixture : StoreGeneratedFixupRelationalFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // H2 has no extensions: the UUID generator the uuid-ossp extension provided upstream
            // is the built-in RANDOM_UUID().
            modelBuilder.Entity<Parent>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("RANDOM_UUID()");
                });

            modelBuilder.Entity<Child>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("RANDOM_UUID()");
                });

            modelBuilder.Entity<ParentPN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ChildPN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ParentDN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ChildDN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ParentNN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ChildNN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<CategoryDN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ProductDN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<CategoryPN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ProductPN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<CategoryNN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<ProductNN>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<Category>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<Product>(
                b =>
                {
                    b.Property(e => e.Id1).ValueGeneratedOnAdd();
                    b.Property(e => e.Id2).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()");
                });

            modelBuilder.Entity<Item>(b => { b.Property(e => e.Id).ValueGeneratedOnAdd(); });

            modelBuilder.Entity<Game>(b => { b.Property(e => e.Id).ValueGeneratedOnAdd().HasDefaultValueSql("uuid_generate_v4()"); });
        }
    }
}
