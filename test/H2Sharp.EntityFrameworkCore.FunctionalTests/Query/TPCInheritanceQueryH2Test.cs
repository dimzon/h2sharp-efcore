namespace H2Sharp.EntityFrameworkCore.Query;

public class TPCInheritanceQueryH2Test(TPCInheritanceQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
    : TPCInheritanceQueryTestBase<TPCInheritanceQueryH2Fixture>(fixture, testOutputHelper)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());
}
