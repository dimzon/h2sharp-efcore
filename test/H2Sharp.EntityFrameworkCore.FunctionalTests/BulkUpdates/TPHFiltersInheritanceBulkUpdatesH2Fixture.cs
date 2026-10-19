namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPHFiltersInheritanceBulkUpdatesH2Fixture : TPHInheritanceBulkUpdatesH2Fixture
{
    public override bool EnableFilters
        => true;
}
