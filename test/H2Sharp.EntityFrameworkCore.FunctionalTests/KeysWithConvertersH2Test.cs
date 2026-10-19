using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class KeysWithConvertersH2Test(KeysWithConvertersH2Test.KeysWithConvertersH2Fixture fixture)
    : KeysWithConvertersTestBase<
        KeysWithConvertersH2Test.KeysWithConvertersH2Fixture>(fixture)
{
    public class KeysWithConvertersH2Fixture : KeysWithConvertersFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder.UseH2(b => b.MinBatchSize(1));
    }
}
