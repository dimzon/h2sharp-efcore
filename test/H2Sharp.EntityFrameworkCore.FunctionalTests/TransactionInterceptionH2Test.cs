using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class TransactionInterceptionH2TestBase(TransactionInterceptionH2TestBase.InterceptionH2FixtureBase fixture)
    : TransactionInterceptionTestBase(fixture)
{
    public abstract class InterceptionH2FixtureBase : InterceptionFixtureBase
    {
        protected override string StoreName
            => "TransactionInterception";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkH2(), injectedInterceptors);
    }

    public class TransactionInterceptionH2Test(TransactionInterceptionH2Test.InterceptionH2Fixture fixture)
        : TransactionInterceptionH2TestBase(fixture), IClassFixture<TransactionInterceptionH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => false;
        }
    }

    public class TransactionInterceptionWithDiagnosticsH2Test(
        TransactionInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture fixture)
        : TransactionInterceptionH2TestBase(fixture),
            IClassFixture<TransactionInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => true;
        }
    }
}
