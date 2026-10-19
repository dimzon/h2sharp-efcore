using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class TPTRelationshipsQueryNpgsqlTest
    : TPTRelationshipsQueryTestBase<TPTRelationshipsQueryNpgsqlTest.TPTRelationshipsQueryNpgsqlFixture>
{
    // ReSharper disable once UnusedParameter.Local
    public TPTRelationshipsQueryNpgsqlTest(
        TPTRelationshipsQueryNpgsqlFixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        fixture.TestSqlLoggerFactory.Clear();
    }

    public class TPTRelationshipsQueryNpgsqlFixture : TPTRelationshipsQueryRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => NpgsqlTestStoreFactory.Instance;
    }
}
