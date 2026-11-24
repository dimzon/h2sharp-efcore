using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using H2Sharp.EntityFrameworkCore.Infrastructure;

namespace H2Sharp.EntityFrameworkCore;

public class H2ApiConsistencyTest(H2ApiConsistencyTest.H2ApiConsistencyFixture fixture)
    : ApiConsistencyTestBase<H2ApiConsistencyTest.H2ApiConsistencyFixture>(fixture)
{
    protected override void AddServices(ServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkH2();

    protected override Assembly TargetAssembly
        => typeof(H2ServiceCollectionExtensions).Assembly;

    public class H2ApiConsistencyFixture : ApiConsistencyFixtureBase
    {
        public override HashSet<Type> FluentApiTypes { get; } =
            [
                typeof(H2DbContextOptionsBuilderExtensions),
                typeof(H2ModelBuilderExtensions),
                typeof(H2PropertyBuilderExtensions),
                typeof(H2ServiceCollectionExtensions)
            ];
    }
}
