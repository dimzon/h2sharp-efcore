using System.Data.Common;

namespace H2Sharp.EntityFrameworkCore.Query;

public class SqlExecutorH2Test : SqlExecutorTestBase<NorthwindQueryH2Fixture<SqlExecutorModelCustomizer>>
{
    public SqlExecutorH2Test(NorthwindQueryH2Fixture<SqlExecutorModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    protected override DbParameter CreateDbParameter(string name, object value)
        => new H2Parameter { ParameterName = name, Value = value };

    protected override string TenMostExpensiveProductsSproc
        => """SELECT * FROM "Ten Most Expensive Products"()""";

    protected override string CustomerOrderHistorySproc
        => """SELECT * FROM "CustOrderHist"($CustomerID)""";

    protected override string CustomerOrderHistoryWithGeneratedParameterSproc
        => """SELECT * FROM "CustOrderHist"({0})""";
}
