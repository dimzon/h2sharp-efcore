namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPTFiltersInheritanceBulkUpdatesNpgsqlFixture : TPTInheritanceBulkUpdatesNpgsqlFixture
{
    public override bool EnableFilters
        => true;
}
