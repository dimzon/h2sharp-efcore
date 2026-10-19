using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.EntityFrameworkCore.TestModels.Northwind;

namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindChangeTrackingQueryH2Test : NorthwindChangeTrackingQueryTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindChangeTrackingQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    protected override NorthwindContext CreateNoTrackingContext()
        => new NorthwindH2Context(
            new DbContextOptionsBuilder(Fixture.CreateOptions())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
}
