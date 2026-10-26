namespace H2Sharp.EntityFrameworkCore;

public class H2ComplianceTest : RelationalComplianceTestBase
{
    // BlockedByPort: this test runs no SQL -- it lists the EF spec bases the provider has not
    // derived yet (compiled model, JSON types, many-to-many loading, ...), so it is the to-do list
    // of test suites rather than a defect of the port. It stays red until those bases are derived.
    [Trait("Category", "BlockedByPort")]
    public override void All_test_bases_must_be_implemented()
        => base.All_test_bases_must_be_implemented();

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
