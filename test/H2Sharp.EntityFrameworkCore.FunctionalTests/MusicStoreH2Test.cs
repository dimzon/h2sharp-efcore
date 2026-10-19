using Microsoft.EntityFrameworkCore.TestModels.MusicStore;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class MusicStoreH2Test(MusicStoreH2Test.MusicStoreH2Fixture fixture)
    : MusicStoreTestBase<MusicStoreH2Test.MusicStoreH2Fixture>(fixture)
{
    public class MusicStoreH2Fixture : MusicStoreFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<CartItem>().Property(s => s.DateCreated).HasColumnType("timestamp without time zone");
            modelBuilder.Entity<Order>().Property(s => s.OrderDate).HasColumnType("timestamp without time zone");
        }
    }
}
