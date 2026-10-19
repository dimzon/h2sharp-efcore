using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public abstract class QueryExpressionInterceptionH2TestBase(
    QueryExpressionInterceptionH2TestBase.InterceptionH2FixtureBase fixture)
    : QueryExpressionInterceptionTestBase(fixture)
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

    public class QueryExpressionInterceptionH2Test(QueryExpressionInterceptionH2Test.InterceptionH2Fixture fixture)
        : QueryExpressionInterceptionH2TestBase(fixture), IClassFixture<QueryExpressionInterceptionH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override string StoreName
                => "QueryExpressionInterception";

            protected override bool ShouldSubscribeToDiagnosticListener
                => false;
        }
    }

    public class QueryExpressionInterceptionWithDiagnosticsH2Test(
        QueryExpressionInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture fixture)
        : QueryExpressionInterceptionH2TestBase(fixture),
            IClassFixture<QueryExpressionInterceptionWithDiagnosticsH2Test.InterceptionH2Fixture>
    {
        public class InterceptionH2Fixture : InterceptionH2FixtureBase
        {
            protected override string StoreName
                => "QueryExpressionInterceptionWithDiagnostics";

            protected override bool ShouldSubscribeToDiagnosticListener
                => true;
        }
    }
}
