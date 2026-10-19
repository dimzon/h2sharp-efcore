namespace H2Sharp.EntityFrameworkCore.Query;

public class TPTFiltersInheritanceQuerySqlServerFixture : TPTInheritanceQueryNpgsqlFixture
{
    public override bool EnableFilters
        => true;
}
