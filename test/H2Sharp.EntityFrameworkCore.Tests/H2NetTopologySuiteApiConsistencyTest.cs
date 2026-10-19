namespace H2Sharp.EntityFrameworkCore;

public class H2NetTopologySuiteApiConsistencyTest(
    H2NetTopologySuiteApiConsistencyTest.H2NetTopologySuiteApiConsistencyFixture fixture)
    : ApiConsistencyTestBase<H2NetTopologySuiteApiConsistencyTest.H2NetTopologySuiteApiConsistencyFixture>(fixture)
{
    protected override void AddServices(ServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkH2NetTopologySuite();

    protected override Assembly TargetAssembly
        => typeof(H2NetTopologySuiteServiceCollectionExtensions).Assembly;

    public class H2NetTopologySuiteApiConsistencyFixture : ApiConsistencyFixtureBase
    {
        public override HashSet<Type> FluentApiTypes { get; } =
            [typeof(H2NetTopologySuiteDbContextOptionsBuilderExtensions), typeof(H2NetTopologySuiteServiceCollectionExtensions)];
    }
}
