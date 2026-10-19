using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class FieldsOnlyLoadH2Test(FieldsOnlyLoadH2Test.FieldsOnlyLoadH2Fixture fixture)
    : FieldsOnlyLoadTestBase<FieldsOnlyLoadH2Test.FieldsOnlyLoadH2Fixture>(fixture)
{
    public class FieldsOnlyLoadH2Fixture : FieldsOnlyLoadFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
