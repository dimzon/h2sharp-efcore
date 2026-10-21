using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The database of the forum: its life and its state.
/// </summary>
public class ForumDatabaseTests
{
    [Fact]
    public void The_tables_are_made_once_and_stay()
    {
        using var database = ForumDatabase.Empty(nameof(The_tables_are_made_once_and_stay));
        using var context = database.Context();

        context.Database.EnsureCreated();
        context.Database.EnsureCreated();

        Assert.Equal(0, context.Forums.Count());
    }

    [Fact]
    public void The_tables_are_dropped_and_made_again()
    {
        using var database = ForumDatabase.Filled(nameof(The_tables_are_dropped_and_made_again));
        using var context = database.Context();

        Assert.Equal(3, context.Forums.Count());

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        Assert.Equal(0, context.Forums.Count());
    }

    [Fact]
    public void The_database_says_whether_it_is_there()
    {
        using var database = ForumDatabase.Empty(nameof(The_database_says_whether_it_is_there));
        using var context = database.Context();

        // The tables are not there yet, and the catalogue says so.
        Assert.Equal(0, Tables(context));

        context.Database.EnsureCreated();

        Assert.Equal(7, Tables(context));
    }

    /// <summary>
    ///     The number of the tables of the forum in the database.
    /// </summary>
    /// <param name="context">The context over the database.</param>
    /// <returns>The number of the tables.</returns>
    private static int Tables(ForumContext context)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'PUBLIC'";

        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public void The_connection_of_the_database_is_the_one_that_was_given()
    {
        using var database = ForumDatabase.Empty(nameof(The_connection_of_the_database_is_the_one_that_was_given));
        using var context = database.Context();

        Assert.Equal("H2Sharp.EntityFrameworkCore", context.Database.ProviderName);
        Assert.NotNull(context.Database.GetDbConnection());
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_number_of_the_rows_is_read_with_a_raw_statement()
    {
        using var database = ForumDatabase.Filled(nameof(The_number_of_the_rows_is_read_with_a_raw_statement));
        using var context = database.Context();

        // The statement is put into a table of its own, "FROM ( SELECT COUNT(*) ... ) AS "t"",
        // and the engine does not read a table of its own yet: "Table "(" not found".
        var count = context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) FROM \"Forums\"")
            .First();

        Assert.Equal(3, count);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_rows_are_read_with_a_raw_statement_that_asks_for_a_number()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_read_with_a_raw_statement_that_asks_for_a_number));
        using var context = database.Context();

        // The statement is put into a table of its own, and the engine does not read a table of
        // its own yet.
        var total = context.Database
            .SqlQueryRaw<int>("SELECT SUM(\"ViewCount\") FROM \"Topics\"")
            .First();

        Assert.Equal(3680, total);
    }
}
