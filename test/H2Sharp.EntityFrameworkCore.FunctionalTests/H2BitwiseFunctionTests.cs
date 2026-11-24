using H2Sharp.EntityFrameworkCore.Storage;

namespace H2Sharp.EntityFrameworkCore;

// H2 2.5 has no boolean XOR operator, but it does have BITXOR over integers. The provider
// casts boolean operands to 1/0, so the same three-valued semantics are checked here.
public class H2BitwiseFunctionTests
{
    [Theory]
    [InlineData("TRUE", "FALSE", true)]
    [InlineData("TRUE", "TRUE", false)]
    [InlineData("FALSE", "FALSE", false)]
    [InlineData("NULL", "TRUE", null)]
    [InlineData("TRUE", "NULL", null)]
    [InlineData("NULL", "NULL", null)]
    public void Boolean_xor_through_BITXOR_handles_null_operands(string left, string right, bool? expected)
    {
        var dataSource = H2DataSources.GetOrCreate(
            "Data Source=bitwise_" + Guid.NewGuid().ToString("N") + ";Storage=Memory");

        using var connection = dataSource.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT BITXOR(CAST({left} AS integer), CAST({right} AS integer)) = 1";

        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());

        if (expected is null)
        {
            Assert.True(reader.IsDBNull(0));
        }
        else
        {
            Assert.False(reader.IsDBNull(0));
            Assert.Equal(expected.Value, reader.GetBoolean(0));
        }

        Assert.False(reader.Read());
    }
}
