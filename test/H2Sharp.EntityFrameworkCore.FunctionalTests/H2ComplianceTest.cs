namespace H2Sharp.EntityFrameworkCore;

public class H2ComplianceTest : RelationalComplianceTestBase
{
    // Every applicable EF spec base now has a derived suite. Postgres-only bases are listed
    // in IgnoredTestBases with the H2 feature they depend on; the rest are compiled and carry
    // BlockedByPort until their individual defects are fixed.
    public override void All_test_bases_must_be_implemented()
        => base.All_test_bases_must_be_implemented();

    protected override ICollection<Type> IgnoredTestBases { get; } = new HashSet<Type>
    {
        // Not implemented
        typeof(CompiledModelTestBase), typeof(CompiledModelRelationalTestBase), // #3087
        typeof(FromSqlSprocQueryTestBase<>),
        typeof(UdfDbFunctionTestBase<>),
        typeof(UpdateSqlGeneratorTestBase),

        // Postgres-only: these bases rely on Npgsql DbFunctions, GlobalTypeMapper and
        // PostGIS types that H2 2.5 does not expose. Their H2 forks stay out of compilation.
        typeof(Microsoft.EntityFrameworkCore.Query.GearsOfWarQueryTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.GearsOfWarFromSqlQueryTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.GearsOfWarQueryRelationalTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.TPCGearsOfWarQueryRelationalTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.TPTGearsOfWarQueryRelationalTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.NorthwindDbFunctionsQueryTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.NorthwindDbFunctionsQueryRelationalTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.NorthwindFunctionsQueryTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.NorthwindFunctionsQueryRelationalTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.SpatialQueryTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.SpatialQueryRelationalTestBase<>),

        // Disabled
        typeof(GraphUpdatesTestBase<>),
        typeof(ProxyGraphUpdatesTestBase<>),
        typeof(OperatorsProceduralQueryTestBase),
    };

    protected override Assembly TargetAssembly { get; } = typeof(H2ComplianceTest).Assembly;
}
