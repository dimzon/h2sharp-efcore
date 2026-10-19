using Microsoft.EntityFrameworkCore.TestModels.ManyToManyModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class ManyToManyTrackingH2Test(ManyToManyTrackingH2Test.ManyToManyTrackingH2Fixture fixture)
    : ManyToManyTrackingRelationalTestBase<
        ManyToManyTrackingH2Test.ManyToManyTrackingH2Fixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class ManyToManyTrackingH2Fixture : ManyToManyTrackingRelationalFixture, ITestSqlLoggerFactory
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // We default to mapping DateTime to 'timestamp with time zone', but the seeding data has Unspecified DateTimes which aren't
            // supported.
            modelBuilder
                .Entity<JoinOneSelfPayload>()
                .Property(e => e.Payload)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            modelBuilder
                .SharedTypeEntity<Dictionary<string, object>>("JoinOneToThreePayloadFullShared")
                .IndexerProperty<string>("Payload")
                .HasDefaultValue("Generated");

            modelBuilder
                .Entity<JoinOneToThreePayloadFull>()
                .Property(e => e.Payload)
                .HasDefaultValue("Generated");

            modelBuilder
                .Entity<UnidirectionalJoinOneSelfPayload>()
                .Property(e => e.Payload)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            modelBuilder
                .SharedTypeEntity<Dictionary<string, object>>("UnidirectionalJoinOneToThreePayloadFullShared")
                .IndexerProperty<string>("Payload")
                .HasDefaultValue("Generated");

            modelBuilder
                .Entity<UnidirectionalJoinOneToThreePayloadFull>()
                .Property(e => e.Payload)
                .HasDefaultValue("Generated");

            // Additional H2-specific config (for timestamp without time zone)
            modelBuilder
                .Entity<UnidirectionalEntityCompositeKey>()
                .Property(e => e.Key3)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<EntityCompositeKey>()
                .Property(e => e.Key3)
                .HasColumnType("timestamp without time zone");
        }
    }
}
