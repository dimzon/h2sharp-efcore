using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class CommandInterceptionH2TestBase(CommandInterceptionH2TestBase.InterceptionH2FixtureBase fixture)
    : CommandInterceptionTestBase(fixture)
{
    public abstract class InterceptionH2FixtureBase : InterceptionFixtureBase
    {
        protected override string StoreName
            => "CommandInterception";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkH2(), injectedInterceptors);
    }

    public class CommandInterceptionH2Test(CommandInterceptionH2Test.InterceptionH2Fixture fixture)
        : CommandInterceptionH2TestBase(fixture), IClassFixture<CommandInterceptionH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
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

    public class CommandInterceptionWithDiagnosticsH2Test(
        CommandInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture fixture)
        : CommandInterceptionH2TestBase(fixture), IClassFixture<CommandInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
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
