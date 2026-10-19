using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class DataAnnotationH2Test(DataAnnotationH2Test.DataAnnotationH2Fixture fixture)
    : DataAnnotationRelationalTestBase<DataAnnotationH2Test.DataAnnotationH2Fixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    protected override TestHelpers TestHelpers
        => H2TestHelpers.Instance;

    public override Task StringLengthAttribute_throws_while_inserting_value_longer_than_max_length()
        => Task.CompletedTask; // H2 does not support length

    public override Task TimestampAttribute_throws_if_value_in_database_changed()
        => Task.CompletedTask; // H2 does not support length

    public override Task MaxLengthAttribute_throws_while_inserting_value_longer_than_max_length()
        => Task.CompletedTask; // H2 does not support length

    public class DataAnnotationH2Fixture : DataAnnotationRelationalFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();
    }
}
