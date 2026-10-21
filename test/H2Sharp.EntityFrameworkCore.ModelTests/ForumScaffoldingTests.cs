using H2Sharp.EntityFrameworkCore.Design.Internal;
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The schema of the forum is read back from the database.
/// </summary>
/// <remarks>
///     The provider makes the tables and the columns, and the same provider reads them back:
///     what the database holds is what the model asked for. The read is the way an application
///     learns the shape of a database it did not make.
/// </remarks>
public class ForumScaffoldingTests
{
    [Fact]
    public void The_tables_of_the_forum_are_read_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_tables_of_the_forum_are_read_back));
        using var context = database.Context();

        var model = Read(context);

        var names = model.Tables.Select(t => t.Name).OrderBy(n => n).ToList();

        Assert.Contains("Forums", names);
        Assert.Contains("Users", names);
        Assert.Contains("Topics", names);
        Assert.Contains("Posts", names);
        Assert.Contains("Tags", names);
        Assert.Contains("TopicTags", names);
        Assert.Contains("Votes", names);
    }

    [Fact]
    public void The_columns_of_a_table_are_read_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_columns_of_a_table_are_read_back));
        using var context = database.Context();

        var model = Read(context);

        var table = model.Tables.Single(t => t.Name == "Forums");
        var columns = table.Columns.Select(c => c.Name).OrderBy(n => n).ToList();

        Assert.Equal(["CreatedAt", "Description", "Id", "IsArchived", "Name"], columns);
    }

    [Fact]
    public void The_types_of_the_columns_are_read_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_types_of_the_columns_are_read_back));
        using var context = database.Context();

        var model = Read(context);

        var table = model.Tables.Single(t => t.Name == "Users");
        var columns = table.Columns.ToDictionary(c => c.Name, c => c.StoreType);

        // The catalogue of H2 names the types in upper case, the way it folds an unquoted name.
        Assert.Equal("INTEGER", columns["Id"]);
        Assert.Equal("TIMESTAMP", columns["JoinedAt"]);

        // The length of the string and the precision of the number are lost: the catalogue of
        // the engine leaves DECLARED_DATA_TYPE empty, and that column is where both are
        // written down.
        Assert.Equal("CHARACTER VARYING", columns["UserName"]);
        Assert.Equal("NUMERIC", columns["Balance"]);
    }

    [Fact]
    public void The_keys_of_a_table_are_read_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_keys_of_a_table_are_read_back));
        using var context = database.Context();

        var model = Read(context);

        var table = model.Tables.Single(t => t.Name == "TopicTags");

        Assert.Equal(["TagId", "TopicId"], table.PrimaryKey!.Columns.Select(c => c.Name).OrderBy(n => n).ToList());
    }

    [Fact]
    public void The_indexes_of_a_table_are_read_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_indexes_of_a_table_are_read_back));
        using var context = database.Context();

        var model = Read(context);

        var table = model.Tables.Single(t => t.Name == "Tags");

        Assert.Contains(table.Indexes, i => i.IsUnique && i.Columns.Count == 1 && i.Columns[0].Name == "Name");
    }

    /// <summary>
    ///     Reads the schema of the database the context is connected to.
    /// </summary>
    /// <param name="context">The context over the database.</param>
    /// <returns>The model of the database.</returns>
    private static DatabaseModel Read(ForumContext context)
    {
        var connection = context.Database.GetDbConnection();

        var services = new ServiceCollection()
            .AddEntityFrameworkH2()
            .AddDbContext<ForumContext>(options => options.UseH2(connection));

        // The services of the time of the design are what hold the reader of the catalogue.
        new H2DesignTimeServices().ConfigureDesignTimeServices(services);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDatabaseModelFactory>();

        return factory.Create(connection, new DatabaseModelFactoryOptions());
    }
}
