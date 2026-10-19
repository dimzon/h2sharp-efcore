using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class TPTRelationshipsQueryH2Test
    : TPTRelationshipsQueryTestBase<TPTRelationshipsQueryH2Test.TPTRelationshipsQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public TPTRelationshipsQueryH2Test(
        TPTRelationshipsQueryH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        fixture.TestSqlLoggerFactory.Clear();
    }

    public class TPTRelationshipsQueryH2Fixture : TPTRelationshipsQueryRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
