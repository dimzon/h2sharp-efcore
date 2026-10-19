namespace H2Sharp.EntityFrameworkCore.Query;

public class TPHFiltersInheritanceQueryNpgsqlFixture : TPHInheritanceQueryNpgsqlFixture
{
    public override bool EnableFilters
        => true;
}
