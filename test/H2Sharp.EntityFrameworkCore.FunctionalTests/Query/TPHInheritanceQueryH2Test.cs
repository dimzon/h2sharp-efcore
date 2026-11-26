using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class TPHInheritanceQueryH2Test(TPHInheritanceQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
    : TPHInheritanceQueryTestBase<TPHInheritanceQueryH2Fixture>(fixture, testOutputHelper);
