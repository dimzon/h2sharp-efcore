namespace H2Sharp.EntityFrameworkCore.BulkUpdates;

public class TPHFiltersInheritanceBulkUpdatesH2Fixture : TPHInheritanceBulkUpdatesH2Fixture
{
    protected override string StoreName
        => "TPHFiltersBulkUpdates";

    public override bool EnableFilters
        => true;
}
