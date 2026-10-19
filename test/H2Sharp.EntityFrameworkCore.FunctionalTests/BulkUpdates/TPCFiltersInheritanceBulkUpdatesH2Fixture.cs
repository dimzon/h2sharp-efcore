namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPCFiltersInheritanceBulkUpdatesH2Fixture : TPCInheritanceBulkUpdatesH2Fixture
{
    public override bool EnableFilters
        => true;
}
