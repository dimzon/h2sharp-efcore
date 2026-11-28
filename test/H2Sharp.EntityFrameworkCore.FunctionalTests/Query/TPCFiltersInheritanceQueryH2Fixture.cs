namespace H2Sharp.EntityFrameworkCore.Query;

public class TPCFiltersInheritanceQueryH2Fixture : TPCInheritanceQueryH2Fixture
{
    // The filtered and unfiltered suites run in parallel and would otherwise share one store.
    protected override string StoreName
        => "TPCFiltersInheritanceTest";

    public override bool EnableFilters
        => true;
}
