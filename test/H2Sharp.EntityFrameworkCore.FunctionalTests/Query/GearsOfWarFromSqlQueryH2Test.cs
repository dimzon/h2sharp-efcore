namespace H2Sharp.EntityFrameworkCore.Query;

public class GearsOfWarFromSqlQueryH2Test : GearsOfWarFromSqlQueryTestBase<GearsOfWarQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public GearsOfWarFromSqlQueryH2Test(GearsOfWarQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public override void From_sql_queryable_simple_columns_out_of_order()
    {
        base.From_sql_queryable_simple_columns_out_of_order();

        Assert.Equal(
            """
SELECT "Id", "Name", "IsAutomatic", "AmmunitionType", "OwnerFullName", "SynergyWithId" FROM "Weapons" ORDER BY "Name"
""",
            Sql);
    }

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();

    private string Sql
        => Fixture.TestSqlLoggerFactory.Sql;
}
