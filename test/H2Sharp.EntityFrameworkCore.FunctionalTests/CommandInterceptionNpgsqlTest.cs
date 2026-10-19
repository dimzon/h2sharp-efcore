using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class CommandInterceptionNpgsqlTestBase(CommandInterceptionNpgsqlTestBase.InterceptionNpgsqlFixtureBase fixture)
    : CommandInterceptionTestBase(fixture)
{
    public abstract class InterceptionNpgsqlFixtureBase : InterceptionFixtureBase
    {
        protected override string StoreName
            => "CommandInterception";

        protected override ITestStoreFactory TestStoreFactory
            => NpgsqlTestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkH2(), injectedInterceptors);
    }

    public class CommandInterceptionNpgsqlTest(CommandInterceptionNpgsqlTest.InterceptionNpgsqlFixture fixture)
        : CommandInterceptionNpgsqlTestBase(fixture), IClassFixture<CommandInterceptionNpgsqlTest.InterceptionNpgsqlFixture>
    {
        public class InterceptionNpgsqlFixture : InterceptionNpgsqlFixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => false;

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                new H2DbContextOptionsBuilder(base.AddOptions(builder))
                    .ExecutionStrategy(d => new NpgsqlExecutionStrategy(d));
                return builder;
            }
        }
    }

    public class CommandInterceptionWithDiagnosticsNpgsqlTest(
        CommandInterceptionWithDiagnosticsNpgsqlTest.InterceptionNpgsqlFixture fixture)
        : CommandInterceptionNpgsqlTestBase(fixture), IClassFixture<CommandInterceptionWithDiagnosticsNpgsqlTest.InterceptionNpgsqlFixture>
    {
        public class InterceptionNpgsqlFixture : InterceptionNpgsqlFixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => true;

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                new H2DbContextOptionsBuilder(base.AddOptions(builder))
                    .ExecutionStrategy(d => new NpgsqlExecutionStrategy(d));
                return builder;
            }
        }
    }
}
