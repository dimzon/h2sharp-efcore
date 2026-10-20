namespace H2Sharp.EntityFrameworkCore;

[Trait("Category", "BlockedByPort")]
    // Every test here needs the engine to run the DDL it generates, and the engine still refuses
    // parts of it: a column default that carries a typed literal, an ALTER TABLE, a sequence, and
    // the FINAL TABLE an identity value comes back through. The identity DDL itself is read since
    // the identity columns were ported. The failures are the port's, not the provider's: see
    // docs/DIVERGENCES.md.
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
