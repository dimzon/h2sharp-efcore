using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class TPCRelationshipsQueryNpgsqlTest
    : TPCRelationshipsQueryTestBase<TPCRelationshipsQueryNpgsqlTest.TPCRelationshipsQueryNpgsqlFixture>
{
    public TPCRelationshipsQueryNpgsqlTest(
        TPCRelationshipsQueryNpgsqlFixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        fixture.TestSqlLoggerFactory.Clear();
    }

    public class TPCRelationshipsQueryNpgsqlFixture : TPCRelationshipsQueryRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => NpgsqlTestStoreFactory.Instance;
    }
}
