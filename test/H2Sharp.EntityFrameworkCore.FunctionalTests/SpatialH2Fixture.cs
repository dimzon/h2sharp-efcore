using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class SpatialH2Fixture : SpatialFixtureBase
{
    // We instruct the test store to pass a connection string to UseH2() instead of a DbConnection - that's required to allow
    // EF's UseNetTopologySuite() to function properly and instantiate an H2DataSource internally.
    protected override ITestStoreFactory TestStoreFactory
        => new H2TestStoreFactory(useConnectionString: true);

    protected override IServiceCollection AddServices(IServiceCollection serviceCollection)
        => base.AddServices(serviceCollection)
            .AddEntityFrameworkH2NetTopologySuite();


    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        modelBuilder.HasPostgresExtension("uuid-ossp");
    }
}
