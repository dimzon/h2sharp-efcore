using H2Sharp.Data;
using Xunit;

namespace H2Sharp.EntityFrameworkCore;

public class RowVersionFinalTableH2Test(ITestOutputHelper output)
{
    [ConditionalFact]
    public void Final_table_returns_the_on_update_value()
    {
        using var connection = new H2Connection("Data Source=RowVersionFinalTable;Storage=Memory");
        connection.Open();

        Execute(connection, "CREATE TABLE T(ID INT PRIMARY KEY, V VARBINARY DEFAULT SECURE_RAND(4) ON UPDATE SECURE_RAND(4), NAME VARCHAR)");
        Execute(connection, "INSERT INTO T(ID, NAME) VALUES (1, 'a')");

        var before = Scalar(connection, "SELECT V FROM T WHERE ID = 1");
        var returned = Scalar(connection, "SELECT V FROM FINAL TABLE (UPDATE T SET NAME = 'b' WHERE ID = 1)");
        var after = Scalar(connection, "SELECT V FROM T WHERE ID = 1");

        output.WriteLine($"before={before}; returned={returned}; after={after}");

        Assert.NotEqual(before, after);
        Assert.Equal(after, returned);
    }

    private static void Execute(H2Connection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static object Scalar(H2Connection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
}
