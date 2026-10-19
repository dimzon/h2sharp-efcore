namespace H2Sharp.EntityFrameworkCore.Query;

public class TPTFiltersInheritanceQuerySqlServerFixture : TPTInheritanceQueryH2Fixture
{
    public override bool EnableFilters
        => true;
}
