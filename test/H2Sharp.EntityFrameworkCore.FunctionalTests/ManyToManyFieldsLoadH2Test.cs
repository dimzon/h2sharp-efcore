using Microsoft.EntityFrameworkCore.TestModels.ManyToManyFieldsModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class ManyToManyFieldsLoadH2Test(ManyToManyFieldsLoadH2Test.ManyToManyFieldsLoadH2Fixture fixture)
    : ManyToManyFieldsLoadTestBase<ManyToManyFieldsLoadH2Test.ManyToManyFieldsLoadH2Fixture>(fixture)
{
    public class ManyToManyFieldsLoadH2Fixture : ManyToManyFieldsLoadFixtureBase, ITestSqlLoggerFactory
    {
        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // We default to mapping DateTime to 'timestamp with time zone', but the seeding data has Unspecified DateTimes which aren't
            // supported.
            modelBuilder.Entity<EntityCompositeKey>()
                .Property(e => e.Key3)
                .HasColumnType("timestamp without time zone");

            modelBuilder
                .Entity<JoinOneSelfPayload>()
                .Property(e => e.Payload)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder
                .SharedTypeEntity<Dictionary<string, object>>("JoinOneToThreePayloadFullShared")
                .IndexerProperty<string>("Payload")
                .HasDefaultValue("Generated");

            modelBuilder
                .Entity<JoinOneToThreePayloadFull>()
                .Property(e => e.Payload)
                .HasDefaultValue("Generated");
        }
    }
}
