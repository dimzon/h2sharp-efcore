namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPCFiltersInheritanceBulkUpdatesH2Fixture : TPCInheritanceBulkUpdatesH2Fixture
{
    protected override string StoreName
        => "TPCFiltersBulkUpdates";

    public override bool EnableFilters
        => true;
}
