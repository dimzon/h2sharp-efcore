namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPTFiltersInheritanceBulkUpdatesH2Fixture : TPTInheritanceBulkUpdatesH2Fixture
{
    protected override string StoreName
        => "TPTFiltersBulkUpdates";

    public override bool EnableFilters
        => true;
}
