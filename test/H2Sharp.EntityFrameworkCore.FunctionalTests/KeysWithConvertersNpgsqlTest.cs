using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class KeysWithConvertersNpgsqlTest(KeysWithConvertersNpgsqlTest.KeysWithConvertersNpgsqlFixture fixture)
    : KeysWithConvertersTestBase<
        KeysWithConvertersNpgsqlTest.KeysWithConvertersNpgsqlFixture>(fixture)
{
    public class KeysWithConvertersNpgsqlFixture : KeysWithConvertersFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => NpgsqlTestStoreFactory.Instance;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder.UseH2(b => b.MinBatchSize(1));
    }
}
