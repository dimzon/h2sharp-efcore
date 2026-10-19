namespace H2Sharp.EntityFrameworkCore;

public class NpgsqlNodaTimeApiConsistencyTest(NpgsqlNodaTimeApiConsistencyTest.NpgsqlNodaTimeApiConsistencyFixture fixture)
    : ApiConsistencyTestBase<NpgsqlNodaTimeApiConsistencyTest.NpgsqlNodaTimeApiConsistencyFixture>(fixture)
{
    protected override void AddServices(ServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkH2NodaTime();

    protected override Assembly TargetAssembly
        => typeof(NpgsqlNodaTimeServiceCollectionExtensions).Assembly;

    public class NpgsqlNodaTimeApiConsistencyFixture : ApiConsistencyFixtureBase
    {
        public override HashSet<Type> FluentApiTypes { get; } =
            [typeof(NpgsqlNodaTimeDbContextOptionsBuilderExtensions), typeof(NpgsqlNodaTimeServiceCollectionExtensions)];
    }
}
