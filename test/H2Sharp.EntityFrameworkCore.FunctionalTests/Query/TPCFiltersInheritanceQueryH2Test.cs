using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class TPCFiltersInheritanceQueryH2Test(TPCFiltersInheritanceQueryH2Fixture fixture)
    : TPCFiltersInheritanceQueryTestBase<TPCFiltersInheritanceQueryH2Fixture>(fixture);
