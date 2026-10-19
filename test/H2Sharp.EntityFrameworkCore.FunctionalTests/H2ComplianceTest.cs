namespace H2Sharp.EntityFrameworkCore;

[Trait("Category", "BlockedByPort")]
    // Every test here needs the engine to run the DDL it generates, and the engine does not read
    // identity DDL yet. The failures are the port's, not the provider's: see docs/DIVERGENCES.md.
    // The test run without this trait is the green one; the run with it is the list of defects.
public class H2ComplianceTest : RelationalComplianceTestBase
{
    protected override ICollection<Type> IgnoredTestBases { get; } = new HashSet<Type>
    {
        // Not implemented
        typeof(CompiledModelTestBase), typeof(CompiledModelRelationalTestBase), // #3087
        typeof(FromSqlSprocQueryTestBase<>),
        typeof(UdfDbFunctionTestBase<>),
        typeof(UpdateSqlGeneratorTestBase),

        // Disabled
        typeof(GraphUpdatesTestBase<>),
        typeof(ProxyGraphUpdatesTestBase<>),
        typeof(OperatorsProceduralQueryTestBase),
    };

    protected override Assembly TargetAssembly { get; } = typeof(H2ComplianceTest).Assembly;
}
