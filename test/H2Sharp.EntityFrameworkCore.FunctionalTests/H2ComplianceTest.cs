namespace H2Sharp.EntityFrameworkCore;

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
