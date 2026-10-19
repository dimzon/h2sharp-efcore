using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class SaveChangesInterceptionNpgsqlTestBase(SaveChangesInterceptionNpgsqlTestBase.InterceptionNpgsqlFixtureBase fixture)
    : SaveChangesInterceptionTestBase(fixture)
{
    public abstract class InterceptionNpgsqlFixtureBase : InterceptionFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => NpgsqlTestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkNpgsql(), injectedInterceptors);

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
        {
            new NpgsqlDbContextOptionsBuilder(base.AddOptions(builder))
                .ExecutionStrategy(d => new NpgsqlExecutionStrategy(d));
            return builder;
        }
    }

    public class SaveChangesInterceptionNpgsqlTest(SaveChangesInterceptionNpgsqlTest.InterceptionNpgsqlFixture fixture)
        : SaveChangesInterceptionNpgsqlTestBase(fixture), IClassFixture<SaveChangesInterceptionNpgsqlTest.InterceptionNpgsqlFixture>
    {
        public class InterceptionNpgsqlFixture : InterceptionNpgsqlFixtureBase
        {
            protected override string StoreName
                => "SaveChangesInterception";

            protected override bool ShouldSubscribeToDiagnosticListener
                => false;

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                new NpgsqlDbContextOptionsBuilder(base.AddOptions(builder))
                    .ExecutionStrategy(d => new NpgsqlExecutionStrategy(d));
                return builder;
            }
        }
    }

    public class SaveChangesInterceptionWithDiagnosticsNpgsqlTest(
        SaveChangesInterceptionWithDiagnosticsNpgsqlTest.InterceptionNpgsqlFixture fixture)
        : SaveChangesInterceptionNpgsqlTestBase(fixture),
            IClassFixture<SaveChangesInterceptionWithDiagnosticsNpgsqlTest.InterceptionNpgsqlFixture>
    {
        public class InterceptionNpgsqlFixture : InterceptionNpgsqlFixtureBase
        {
            protected override string StoreName
                => "SaveChangesInterceptionWithDiagnostics";

            protected override bool ShouldSubscribeToDiagnosticListener
                => true;

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                new NpgsqlDbContextOptionsBuilder(base.AddOptions(builder))
                    .ExecutionStrategy(d => new NpgsqlExecutionStrategy(d));
                return builder;
            }
        }
    }
}
