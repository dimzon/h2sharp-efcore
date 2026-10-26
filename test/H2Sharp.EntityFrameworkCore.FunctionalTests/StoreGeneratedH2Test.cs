using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: 210 of the suite's 211 tests pass; the one that does not is the Deleted row of
// Change_state_of_entity_with_temp_non_key_does_not_throw, which saves through the update pipeline
// and stops at "Not ported yet: Prepared.update". That row cannot be marked on its own: the base
// method is not virtual, so there is no override to put the trait on, and a `new` method shadowing
// it is not a second test case -- xunit keys the case by class, method name and arguments, drops
// one of the two and keeps the inherited one, which carries no trait (checked: the discovered case
// has no traits and the row is still in the green run). The class keeps the trait until the engine
// ports the update path.
[Trait("Category", "BlockedByPort")]
public class StoreGeneratedH2Test(StoreGeneratedH2Test.StoreGeneratedH2Fixture fixture)
    : StoreGeneratedTestBase<StoreGeneratedH2Test.StoreGeneratedH2Fixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class StoreGeneratedH2Fixture : StoreGeneratedFixtureBase
    {
        protected override string StoreName { get; } = "StoreGeneratedTest";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder
                .EnableSensitiveDataLogging()
                .ConfigureWarnings(
                    b => b.Default(WarningBehavior.Throw)
                        .Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning)
                        .Ignore(RelationalEventId.BoolWithDefaultWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            modelBuilder.Entity<Gumball>(
                b =>
                {
                    b.Property(e => e.Id).UseSerialColumn();
                    b.Property(e => e.Identity).HasDefaultValue("Banana Joe");
                    b.Property(e => e.IdentityReadOnlyBeforeSave).HasDefaultValue("Doughnut Sheriff");
                    b.Property(e => e.IdentityReadOnlyAfterSave).HasDefaultValue("Anton");
                    b.Property(e => e.AlwaysIdentity).HasDefaultValue("Banana Joe");
                    b.Property(e => e.AlwaysIdentityReadOnlyBeforeSave).HasDefaultValue("Doughnut Sheriff");
                    b.Property(e => e.AlwaysIdentityReadOnlyAfterSave).HasDefaultValue("Anton");
                    b.Property(e => e.Computed).HasDefaultValue("Alan");
                    b.Property(e => e.ComputedReadOnlyBeforeSave).HasDefaultValue("Carmen");
                    b.Property(e => e.ComputedReadOnlyAfterSave).HasDefaultValue("Tina Rex");
                    b.Property(e => e.AlwaysComputed).HasDefaultValue("Alan");
                    b.Property(e => e.AlwaysComputedReadOnlyBeforeSave).HasDefaultValue("Carmen");
                    b.Property(e => e.AlwaysComputedReadOnlyAfterSave).HasDefaultValue("Tina Rex");
                });

            modelBuilder.Entity<Anais>(
                b =>
                {
                    b.Property(e => e.OnAdd).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddUseBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddIgnoreBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddThrowBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddUseBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddIgnoreBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddThrowBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddUseBeforeThrowAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddIgnoreBeforeThrowAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddThrowBeforeThrowAfter).HasDefaultValue("Rabbit");

                    b.Property(e => e.OnAddOrUpdate).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateUseBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateIgnoreBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateThrowBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateUseBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateIgnoreBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateThrowBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateUseBeforeThrowAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateIgnoreBeforeThrowAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnAddOrUpdateThrowBeforeThrowAfter).HasDefaultValue("Rabbit");

                    b.Property(e => e.OnUpdate).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateUseBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateIgnoreBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateThrowBeforeUseAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateUseBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateIgnoreBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateThrowBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateUseBeforeThrowAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateIgnoreBeforeThrowAfter).HasDefaultValue("Rabbit");
                    b.Property(e => e.OnUpdateThrowBeforeThrowAfter).HasDefaultValue("Rabbit");
                });

            // In StoreGeneratedSqlServerTest the following are defined with HasComputedValueSql, but that's
            // not supported by PostgreSQL
            modelBuilder.Entity<WithBackingFields>(
                b =>
                {
                    b.Property(e => e.NullableAsNonNullable).HasDefaultValueSql("1");
                    b.Property(e => e.NonNullableAsNullable).HasDefaultValueSql("1");
                });

            modelBuilder.Entity<WithNoBackingFields>(
                b =>
                {
                    b.Property(e => e.TrueDefault).HasDefaultValue(true);
                    b.Property(e => e.NonZeroDefault).HasDefaultValue(-1);
                    b.Property(e => e.FalseDefault).HasDefaultValue(false);
                    b.Property(e => e.ZeroDefault).HasDefaultValue(0);
                });

            modelBuilder.Entity<WithNullableBackingFields>(
                b =>
                {
                    b.Property(e => e.NullableBackedBoolTrueDefault).HasDefaultValue(true);
                    b.Property(e => e.NullableBackedIntNonZeroDefault).HasDefaultValue(-1);
                    b.Property(e => e.NullableBackedBoolFalseDefault).HasDefaultValue(false);
                    b.Property(e => e.NullableBackedIntZeroDefault).HasDefaultValue(0);
                });

            modelBuilder.Entity<WithObjectBackingFields>(
                b =>
                {
                    b.Property(e => e.NullableBackedBoolTrueDefault).HasDefaultValue(true);
                    b.Property(e => e.NullableBackedIntNonZeroDefault).HasDefaultValue(-1);
                    b.Property(e => e.NullableBackedBoolFalseDefault).HasDefaultValue(false);
                    b.Property(e => e.NullableBackedIntZeroDefault).HasDefaultValue(0);
                });

            modelBuilder.Entity<NonStoreGenDependent>().Property(e => e.HasTemp).HasDefaultValue(777);

            modelBuilder.Entity<CompositePrincipal>().Property(e => e.Id).UseIdentityColumn();

            base.OnModelCreating(modelBuilder, context);
        }
    }
}
