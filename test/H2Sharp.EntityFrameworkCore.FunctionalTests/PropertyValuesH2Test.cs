using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the suite saves through the batch, and the port's Prepared update path is a
// stub ("Not ported yet: Prepared.update"), so every save fails at the engine.
[Trait("Category", "BlockedByPort")]
public class PropertyValuesH2Test(PropertyValuesH2Test.PropertyValuesH2Fixture fixture)
    : PropertyValuesTestBase<PropertyValuesH2Test.PropertyValuesH2Fixture>(fixture)
{
    public class PropertyValuesH2Fixture : PropertyValuesFixtureBase
    {
        protected override string StoreName { get; } = "PropertyValues";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // We default to mapping DateTime to 'timestamp with time zone', but the seeding data has Unspecified DateTimes which aren't
            // supported.
            modelBuilder.Entity<PastEmployee>().Property(e => e.TerminationDate).HasColumnType("timestamp without time zone");

            modelBuilder.Entity<Building>()
                .Property(b => b.Value).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CurrentEmployee>()
                .Property(ce => ce.LeaveBalance).HasColumnType("decimal(18,2)");
        }
    }
}
