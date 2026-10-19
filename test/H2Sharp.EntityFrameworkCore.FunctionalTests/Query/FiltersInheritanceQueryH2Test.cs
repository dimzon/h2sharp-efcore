namespace H2Sharp.EntityFrameworkCore.Query;

public class FiltersInheritanceQueryH2Test : FiltersInheritanceQueryTestBase<TPHFiltersInheritanceQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public FiltersInheritanceQueryH2Test(TPHFiltersInheritanceQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }
}
