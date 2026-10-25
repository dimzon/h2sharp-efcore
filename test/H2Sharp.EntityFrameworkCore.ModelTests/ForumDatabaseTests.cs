using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;
using H2Sharp.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

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

    [Fact]
    public void The_tables_are_asked_for_before_and_after_they_are_made()
    {
        using var database = ForumDatabase.Empty(nameof(The_tables_are_asked_for_before_and_after_they_are_made));
        using var context = database.Context();
        var creator = context.GetService<IRelationalDatabaseCreator>();

        // The catalogue of an empty database has no tables of the forum in it.
        Assert.False(creator.HasTables());

        context.Database.EnsureCreated();

        Assert.True(creator.HasTables());
        Assert.True(creator.Exists());
        Assert.True(context.Database.CanConnect());
    }

    [Fact]
    public async Task The_tables_are_asked_for_asynchronously()
    {
        using var database = ForumDatabase.Empty(nameof(The_tables_are_asked_for_asynchronously));
        await using var context = database.Context();
        var creator = context.GetService<IRelationalDatabaseCreator>();

        Assert.False(await creator.HasTablesAsync());

        await context.Database.EnsureCreatedAsync();

        Assert.True(await creator.HasTablesAsync());
        Assert.True(await creator.ExistsAsync());
    }

    [Fact]
    public async Task The_database_is_made_and_deleted_asynchronously()
    {
        using var database = ForumDatabase.Empty(nameof(The_database_is_made_and_deleted_asynchronously));
        await using var context = database.Context();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        Assert.Equal(0, await context.Forums.CountAsync());

        await context.Database.EnsureDeletedAsync();

        Assert.False(context.GetService<IRelationalDatabaseCreator>().HasTables());
    }

    [Fact]
    public void A_schema_of_its_own_is_made_and_found()
    {
        using var connection = new H2Connection("jdbc:h2:mem:forum_schema_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new SchemaContext(
            new DbContextOptionsBuilder<SchemaContext>().UseH2(connection).Options);
        var creator = context.GetService<IRelationalDatabaseCreator>();

        // The schema the model names is not there until the creator makes it; the default
        // schema, PUBLIC, is made with the database and is never reported missing.
        Assert.False(creator.Exists());

        creator.Create();

        Assert.True(creator.Exists());
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void A_unique_constraint_is_added_to_an_existing_table()
    {
        using var database = ForumDatabase.Empty(nameof(A_unique_constraint_is_added_to_an_existing_table));
        using var context = database.Context();
        context.Database.EnsureCreated();

        var commands = context.GetService<IMigrationsSqlGenerator>().Generate(
        [
            new AddUniqueConstraintOperation
            {
                Table = "Forums",
                Name = "UQ_Forums_Name",
                Columns = ["Name"]
            }
        ]);

        var statement = string.Join("\n", commands.Select(command => command.CommandText + ";"));

        // The reference reads ALTER TABLE ... ADD CONSTRAINT ... UNIQUE; the port reads the
        // constraint clause only for PRIMARY KEY and answers `expected "PRIMARY"`.
        var reference = H2ReferenceOracle.Run(context.Database.GenerateCreateScript() + statement);
        Assert.True(reference.Accepted, reference.Output);

        foreach (var command in commands)
        {
            context.Database.ExecuteSqlRaw(command.CommandText);
        }
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

    private sealed class Thought
    {
        public int Id { get; set; }
    }

    private sealed class SchemaContext(DbContextOptions<SchemaContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("forum_notes");
            modelBuilder.Entity<Thought>().HasKey(e => e.Id);
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
    public void The_number_of_the_rows_is_read_with_a_raw_statement()
    {
        using var database = ForumDatabase.Filled(nameof(The_number_of_the_rows_is_read_with_a_raw_statement));
        using var context = database.Context();

        // EF reads the first column of the statement and names it "Value": the raw
        // statement must name it so, or the derived table has no column to read.
        var count = context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM \"Forums\"")
            .First();

        Assert.Equal(3, count);
    }

    [Fact]
    public void The_rows_are_read_with_a_raw_statement_that_asks_for_a_number()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_read_with_a_raw_statement_that_asks_for_a_number));
        using var context = database.Context();

        var total = context.Database
            .SqlQueryRaw<int>("SELECT SUM(\"ViewCount\") AS \"Value\" FROM \"Topics\"")
            .First();

        Assert.Equal(3680, total);
    }
}
