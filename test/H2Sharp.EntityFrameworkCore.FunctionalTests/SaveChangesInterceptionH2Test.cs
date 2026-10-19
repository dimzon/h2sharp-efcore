using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class SaveChangesInterceptionH2TestBase(SaveChangesInterceptionH2TestBase.InterceptionH2FixtureBase fixture)
    : SaveChangesInterceptionTestBase(fixture)
{
    public abstract class InterceptionH2FixtureBase : InterceptionFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkH2(), injectedInterceptors);

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
        {
            new H2DbContextOptionsBuilder(base.AddOptions(builder))
                .ExecutionStrategy(d => new H2ExecutionStrategy(d));
            return builder;
        }
    }

    public class SaveChangesInterceptionH2Test(SaveChangesInterceptionH2Test.InterceptionH2Fixture fixture)
        : SaveChangesInterceptionH2TestBase(fixture), IClassFixture<SaveChangesInterceptionH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override string StoreName
                => "SaveChangesInterception";

            protected override bool ShouldSubscribeToDiagnosticListener
                => false;

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                new H2DbContextOptionsBuilder(base.AddOptions(builder))
                    .ExecutionStrategy(d => new H2ExecutionStrategy(d));
                return builder;
            }
        }
    }

    public class SaveChangesInterceptionWithDiagnosticsH2Test(
        SaveChangesInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture fixture)
        : SaveChangesInterceptionH2TestBase(fixture),
            IClassFixture<SaveChangesInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override string StoreName
                => "SaveChangesInterceptionWithDiagnostics";

            protected override bool ShouldSubscribeToDiagnosticListener
                => true;

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                new H2DbContextOptionsBuilder(base.AddOptions(builder))
                    .ExecutionStrategy(d => new H2ExecutionStrategy(d));
                return builder;
            }
        }
    }
}
