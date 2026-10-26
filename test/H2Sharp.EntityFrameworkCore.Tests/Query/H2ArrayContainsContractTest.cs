#nullable enable
using System.Globalization;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

/// <summary>
///     Pins what the engine answers when the provider asks whether an array holds a NULL element.
/// </summary>
/// <remarks>
///     <para>
///         <c>H2SqlNullabilityProcessor</c> compensates <c>nullable = ANY(array)</c> by asking exactly
///         that question, and the answer decides whether rows are kept: a wrong answer silently drops or
///         keeps rows instead of failing the query. H2 has no <c>array_position</c> -- the PostgreSQL
///         function the compensation was ported with -- so the question is asked with
///         <c>ARRAY_CONTAINS(a, NULL)</c>.
///     </para>
///     <para>
///         These cases are the whole contract: a NULL element answers TRUE, a missing one answers FALSE,
///         an empty array answers FALSE, a NULL array answers NULL, and the COALESCE the provider wraps
///         around the call turns that NULL into FALSE so the expression stays non-nullable, as the
///         <c>array_position(...) IS NOT NULL</c> form it replaced was. When the reference jar is
///         available the same expressions run on it and must agree: the port is what we ship, but H2 is
///         what the provider targets. See <c>docs/BACKLOG.md</c>, item 2.
///     </para>
/// </remarks>
public class H2ArrayContainsContractTest
{
    [Theory]
    [InlineData("ARRAY_CONTAINS(ARRAY[1, NULL, 3], NULL)", "TRUE")]
    [InlineData("ARRAY_CONTAINS(ARRAY[1, 2, 3], NULL)", "FALSE")]
    [InlineData("ARRAY_CONTAINS(ARRAY[]::INT ARRAY, NULL)", "FALSE")]
    [InlineData("ARRAY_CONTAINS(ARRAY[NULL], NULL)", "TRUE")]
    [InlineData("ARRAY_CONTAINS(ARRAY['a', NULL], NULL)", "TRUE")]
    [InlineData("ARRAY_CONTAINS(CAST(NULL AS INT ARRAY), NULL)", null)]
    [InlineData("COALESCE(ARRAY_CONTAINS(CAST(NULL AS INT ARRAY), NULL), FALSE)", "FALSE")]
    public void Array_contains_answers_whether_the_array_holds_a_null(string expression, string? expected)
    {
        Assert.Equal(expected, Scalar($"SELECT {expression}"));

        if (H2ReferenceOracle.IsAvailable)
        {
            var rows = H2ReferenceOracle.Query("", $"SELECT {expression};");
            Assert.Equal(expected, Assert.Single(Assert.Single(rows)));
        }
    }

    private static string? Scalar(string sql)
    {
        using var connection = new H2Connection("jdbc:h2:mem:array_contains_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        return command.ExecuteScalar() switch
        {
            null or DBNull => null,
            bool value => value ? "TRUE" : "FALSE",
            var value => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }
}
