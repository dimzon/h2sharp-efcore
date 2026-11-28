namespace H2Sharp.EntityFrameworkCore.Query;

public class TPHFiltersInheritanceQueryH2Fixture : TPHInheritanceQueryH2Fixture
{
    protected override string StoreName
        => "TPHFiltersInheritanceTest";

    public override bool EnableFilters
        => true;
}
