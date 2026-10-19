namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPCFiltersInheritanceBulkUpdatesNpgsqlFixture : TPCInheritanceBulkUpdatesNpgsqlFixture
{
    public override bool EnableFilters
        => true;
}
