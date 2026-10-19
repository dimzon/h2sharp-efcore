using Microsoft.EntityFrameworkCore.TestModels.MusicStore;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class MusicStoreNpgsqlTest(MusicStoreNpgsqlTest.MusicStoreNpgsqlFixture fixture)
    : MusicStoreTestBase<MusicStoreNpgsqlTest.MusicStoreNpgsqlFixture>(fixture)
{
    public class MusicStoreNpgsqlFixture : MusicStoreFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => NpgsqlTestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<CartItem>().Property(s => s.DateCreated).HasColumnType("timestamp without time zone");
            modelBuilder.Entity<Order>().Property(s => s.OrderDate).HasColumnType("timestamp without time zone");
        }
    }
}
