using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class NotificationEntitiesH2Test(NotificationEntitiesH2Test.NotificationEntitiesH2Fixture fixture)
    : NotificationEntitiesTestBase<NotificationEntitiesH2Test.NotificationEntitiesH2Fixture>(fixture)
{
    public class NotificationEntitiesH2Fixture : NotificationEntitiesFixtureBase
    {
        protected override string StoreName { get; } = "NotificationEntities";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
