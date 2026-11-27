using System.Data.Common;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class SqlExecutorH2Test : SqlExecutorTestBase<NorthwindQueryH2Fixture<SqlExecutorModelCustomizer>>
{
    public SqlExecutorH2Test(NorthwindQueryH2Fixture<SqlExecutorModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // The base passes PostgreSQL-style '@name' parameters; the H2 dialect writes '$name'.
    protected override DbParameter CreateDbParameter(string name, object value)
        => new H2Parameter { ParameterName = name?.TrimStart('@'), Value = value };

    // H2 has no stored procedures. These two SQL Server/PG function calls are Northwind
    // fixtures, not H2 objects, so the calls stay in the blocked set with their reason.
    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "BlockedByPort")]
    public override Task Executes_stored_procedure(bool async)
        => base.Executes_stored_procedure(async);

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "BlockedByPort")]
    public override Task Executes_stored_procedure_with_parameter(bool async)
        => base.Executes_stored_procedure_with_parameter(async);

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "BlockedByPort")]
    public override Task Executes_stored_procedure_with_generated_parameter(bool async)
        => base.Executes_stored_procedure_with_generated_parameter(async);

    protected override string TenMostExpensiveProductsSproc
        => """SELECT * FROM "Ten Most Expensive Products"()""";

    protected override string CustomerOrderHistorySproc
        => """SELECT * FROM "CustOrderHist"($CustomerID)""";

    protected override string CustomerOrderHistoryWithGeneratedParameterSproc
        => """SELECT * FROM "CustOrderHist"({0})""";
}
