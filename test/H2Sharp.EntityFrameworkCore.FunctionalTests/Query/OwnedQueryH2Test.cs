using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class OwnedQueryH2Test(OwnedQueryH2Test.OwnedQueryH2Fixture fixture)
    : OwnedQueryRelationalTestBase<OwnedQueryH2Test.OwnedQueryH2Fixture>(fixture)
{
    public class OwnedQueryH2Fixture : RelationalOwnedQueryFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // We default to mapping DateTime to 'timestamp with time zone', but the seeding data has Unspecified DateTimes which aren't
            // supported.
            modelBuilder.Entity<OwnedPerson>(
                eb => eb.OwnsMany(
                    p => p.Orders, ob => ob.IndexerProperty<DateTime>("OrderDate").HasColumnType("timestamp without time zone")));
        }
    }
}
