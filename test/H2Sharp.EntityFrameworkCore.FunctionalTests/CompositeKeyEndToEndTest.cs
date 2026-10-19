using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class CompositeKeyEndToEndNpgsqlTest(CompositeKeyEndToEndNpgsqlTest.CompositeKeyEndToEndNpgsqlFixture fixture)
    : CompositeKeyEndToEndTestBase<CompositeKeyEndToEndNpgsqlTest.CompositeKeyEndToEndNpgsqlFixture>(fixture)
{
    public class CompositeKeyEndToEndNpgsqlFixture : CompositeKeyEndToEndFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => NpgsqlTestStoreFactory.Instance;
    }
}
