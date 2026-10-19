using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class TPCRelationshipsQueryH2Test
    : TPCRelationshipsQueryTestBase<TPCRelationshipsQueryH2Test.TPCRelationshipsQueryH2Fixture>
{
    public TPCRelationshipsQueryH2Test(
        TPCRelationshipsQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        fixture.TestSqlLoggerFactory.Clear();
    }

    public class TPCRelationshipsQueryH2Fixture : TPCRelationshipsQueryRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
