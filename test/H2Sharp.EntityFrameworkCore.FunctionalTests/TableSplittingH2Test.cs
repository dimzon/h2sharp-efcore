using Microsoft.EntityFrameworkCore.TestModels.TransportationModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

[MinimumPostgresVersion(12, 0)] // Test suite uses computed columns
public class TableSplittingH2Test(ITestOutputHelper testOutputHelper) : TableSplittingTestBase(testOutputHelper)
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;

    public override async Task ExecuteUpdate_works_for_table_sharing(bool async)
    {
        await base.ExecuteUpdate_works_for_table_sharing(async);

        AssertSql(
            """
UPDATE "Vehicles" AS v
SET "SeatingCapacity" = 1
""",
            //
            """
SELECT NOT EXISTS (
    SELECT 1
    FROM "Vehicles" AS v
    WHERE v."SeatingCapacity" <> 1)
""");
    }

}
