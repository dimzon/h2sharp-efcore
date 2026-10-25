using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the suite saves through the batch, and the port's Prepared update path is a
// stub ("Not ported yet: Prepared.update"), so every save fails at the engine.
[Trait("Category", "BlockedByPort")]
public class FieldMappingH2Test(FieldMappingH2Test.FieldMappingH2Fixture fixture)
    : FieldMappingTestBase<FieldMappingH2Test.FieldMappingH2Fixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class FieldMappingH2Fixture : FieldMappingFixtureBase
    {
        protected override string StoreName { get; } = "FieldMapping";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
