namespace H2Sharp.EntityFrameworkCore;

public class H2NodaTimeApiConsistencyTest(H2NodaTimeApiConsistencyTest.H2NodaTimeApiConsistencyFixture fixture)
    : ApiConsistencyTestBase<H2NodaTimeApiConsistencyTest.H2NodaTimeApiConsistencyFixture>(fixture)
{
    protected override void AddServices(ServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkH2NodaTime();

    protected override Assembly TargetAssembly
        => typeof(H2NodaTimeServiceCollectionExtensions).Assembly;

    public class H2NodaTimeApiConsistencyFixture : ApiConsistencyFixtureBase
    {
        public override HashSet<Type> FluentApiTypes { get; } =
            [typeof(H2NodaTimeDbContextOptionsBuilderExtensions), typeof(H2NodaTimeServiceCollectionExtensions)];
    }
}
