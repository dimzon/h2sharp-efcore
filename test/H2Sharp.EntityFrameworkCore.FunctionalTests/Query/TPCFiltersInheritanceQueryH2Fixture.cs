namespace H2Sharp.EntityFrameworkCore.Query;

public class TPCFiltersInheritanceQueryH2Fixture : TPCInheritanceQueryH2Fixture
{
    public override bool EnableFilters
        => true;
}
