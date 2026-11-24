using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

// The two procedural operator tests are commented out in the EF Core 9 base, so this class has no
// runnable methods; it exists to satisfy the compliance check that the provider has a derivation.
public class H2OperatorsProceduralQueryTest : OperatorsProceduralQueryTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
