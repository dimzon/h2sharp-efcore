using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class TPTInheritanceQueryH2Test(TPTInheritanceQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
    : TPTInheritanceQueryTestBase<TPTInheritanceQueryH2Fixture>(fixture, testOutputHelper);
