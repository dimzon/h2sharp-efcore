using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class WithConstructorsH2Test(WithConstructorsH2Test.WithConstructorsH2Fixture fixture)
    : WithConstructorsTestBase<WithConstructorsH2Test.WithConstructorsH2Fixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class WithConstructorsH2Fixture : WithConstructorsFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<BlogQuery>().HasNoKey().ToSqlQuery("""
                SELECT * FROM "Blog"
                """);
        }
    }
}
