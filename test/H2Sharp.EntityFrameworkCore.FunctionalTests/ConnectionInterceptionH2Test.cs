using System.Data;
using System.Data.Common;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class ConnectionInterceptionH2TestBase(ConnectionInterceptionH2TestBase.InterceptionH2FixtureBase fixture)
    : ConnectionInterceptionTestBase(fixture)
{
    [ConditionalTheory(Skip = "#2368")]
    public override Task Intercept_connection_creation_passively(bool async)
        => base.Intercept_connection_creation_passively(async);

    [ConditionalTheory(Skip = "#2368")]
    public override Task Intercept_connection_creation_with_multiple_interceptors(bool async)
        => base.Intercept_connection_creation_with_multiple_interceptors(async);

    [ConditionalTheory(Skip = "#2368")]
    public override Task Intercept_connection_to_override_connection_after_creation(bool async)
        => base.Intercept_connection_to_override_connection_after_creation(async);

    [ConditionalTheory(Skip = "#2368")]
    public override Task Intercept_connection_to_override_creation(bool async)
        => base.Intercept_connection_to_override_creation(async);

    public abstract class InterceptionH2FixtureBase : InterceptionFixtureBase
    {
        protected override string StoreName
            => "ConnectionInterception";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override IServiceCollection InjectInterceptors(
            IServiceCollection serviceCollection,
            IEnumerable<IInterceptor> injectedInterceptors)
            => base.InjectInterceptors(serviceCollection.AddEntityFrameworkH2(), injectedInterceptors);
    }

    protected override DbContextOptionsBuilder ConfigureProvider(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseH2();

    protected override BadUniverseContext CreateBadUniverse(DbContextOptionsBuilder optionsBuilder)
        => new(optionsBuilder.UseH2(new FakeDbConnection()).Options);

    public class FakeDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; }

        public override string Database
            => "Database";

        public override string DataSource
            => "DataSource";

        public override string ServerVersion
            => throw new NotImplementedException();

        public override ConnectionState State
            => ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName)
            => throw new NotImplementedException();

        public override void Close()
            => throw new NotImplementedException();

        public override void Open()
            => throw new NotImplementedException();

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => throw new NotImplementedException();

        protected override DbCommand CreateDbCommand()
            => throw new NotImplementedException();
    }

    public class ConnectionInterceptionH2Test(ConnectionInterceptionH2Test.InterceptionH2Fixture fixture)
        : ConnectionInterceptionH2TestBase(fixture), IClassFixture<ConnectionInterceptionH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => false;
        }
    }

    public class ConnectionInterceptionWithDiagnosticsH2Test(
        ConnectionInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture fixture)
        : ConnectionInterceptionH2TestBase(fixture),
            IClassFixture<ConnectionInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override bool ShouldSubscribeToDiagnosticListener
                => true;
        }
    }
}
