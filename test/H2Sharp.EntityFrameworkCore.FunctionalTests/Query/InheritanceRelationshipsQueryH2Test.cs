using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class InheritanceRelationshipsQueryH2Test(InheritanceRelationshipsQueryH2Fixture fixture)
    : InheritanceRelationshipsQueryTestBase<InheritanceRelationshipsQueryH2Fixture>(fixture)
{
    protected override void ClearLog() { }
}
