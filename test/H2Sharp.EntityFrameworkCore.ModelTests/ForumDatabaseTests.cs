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
    public void An_identity_key_of_the_wrong_type_is_refused()
    {
        using var connection = new H2Connection("jdbc:h2:mem:wrong_identity_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new WrongIdentityContext(
            new DbContextOptionsBuilder<WrongIdentityContext>().UseH2(connection).Options);

        // An identity column counts in whole numbers: a text key cannot be one.
        var error = Assert.Throws<InvalidOperationException>(() => context.Database.EnsureCreated());

        Assert.Contains("Code", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_index_include_of_a_missing_column_is_refused()
    {
        using var connection = new H2Connection("jdbc:h2:mem:wrong_include_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new WrongIncludeContext(
            new DbContextOptionsBuilder<WrongIncludeContext>().UseH2(connection).Options);

        // The included column must belong to the indexed table, or the index is nonsense.
        var error = Assert.Throws<InvalidOperationException>(() => context.Database.EnsureCreated());

        Assert.Contains("Missing", error.Message, StringComparison.Ordinal);
    }

    private sealed class WrongIdentity
    {
        public string Code { get; set; } = "";
    }

    private sealed class WrongIdentityContext(DbContextOptions<WrongIdentityContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WrongIdentity>().HasKey(e => e.Code);
            modelBuilder.Entity<WrongIdentity>().Property(e => e.Code).UseIdentityByDefaultColumn();
        }
    }

    private sealed class WrongInclude
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class WrongIncludeContext(DbContextOptions<WrongIncludeContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WrongInclude>().HasKey(e => e.Id);
            modelBuilder.Entity<WrongInclude>().HasIndex(e => e.Name).IncludeProperties("Missing");
        }
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
