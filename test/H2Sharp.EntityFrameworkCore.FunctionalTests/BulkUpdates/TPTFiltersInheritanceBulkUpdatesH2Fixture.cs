namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPTFiltersInheritanceBulkUpdatesH2Fixture : TPTInheritanceBulkUpdatesH2Fixture
{
    public override bool EnableFilters
        => true;
}
