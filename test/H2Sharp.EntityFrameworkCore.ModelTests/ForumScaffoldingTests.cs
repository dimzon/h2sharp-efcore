using System.Data.Common;
using H2Sharp.EntityFrameworkCore.Design.Internal;
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;

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

    [Fact]
    public void The_model_is_scaffolded_back_with_fluent_calls()
    {
        using var database = ForumDatabase.Filled(nameof(The_model_is_scaffolded_back_with_fluent_calls));
        using var context = database.Context();

        var services = new ServiceCollection()
            .AddEntityFrameworkH2()
            .AddDbContext<ForumContext>(options => options.UseH2(context.Database.GetDbConnection()));

        new H2DesignTimeServices().ConfigureDesignTimeServices(services);

        IServiceProvider? provider = null;
        services.AddEntityFrameworkDesignTimeServices(new QuietReporter(), () => provider!);
        provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var scaffolder = scope.ServiceProvider.GetRequiredService<IReverseEngineerScaffolder>();

        var result = scaffolder.ScaffoldModel(
            context.Database.GetConnectionString()!,
            new DatabaseModelFactoryOptions(),
            new ModelReverseEngineerOptions(),
            new ModelCodeGenerationOptions { RootNamespace = "Scaffolded" });

        Assert.NotNull(result.ContextFile);
        Assert.Contains("UseH2", result.ContextFile.Code, StringComparison.Ordinal);
        Assert.Contains("DbSet<User>", result.ContextFile.Code, StringComparison.Ordinal);

        // Every table comes back as a class of its own, with the columns it has.
        var userFile = result.AdditionalFiles.Select(f => f.Code).First(code => code.Contains("UserName", StringComparison.Ordinal));
        Assert.Contains("partial class User", userFile, StringComparison.Ordinal);
        Assert.Contains("UserName", userFile, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Silent operation reporting for design-time services used outside the tools.
    /// </summary>
    private sealed class QuietReporter : IOperationReporter
    {
        public void WriteError(string message)
        {
        }

        public void WriteInformation(string message)
        {
        }

        public void WriteVerbose(string message)
        {
        }

        public void WriteWarning(string message)
        {
        }
    }

    [Fact]
    public void The_sequences_of_the_database_are_read_back()
    {
        using var connection = new H2Connection("jdbc:h2:mem:scaffold_sequences_" + Guid.NewGuid().ToString("N"));
        connection.Open();

        var options = new DbContextOptionsBuilder<DraftContext>().UseH2(connection).Options;
        using (var context = new DraftContext(options))
        {
            context.Database.EnsureCreated();
        }

        var services = new ServiceCollection()
            .AddEntityFrameworkH2()
            .AddDbContext<DraftContext>(options => options.UseH2(connection));

        new H2DesignTimeServices().ConfigureDesignTimeServices(services);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDatabaseModelFactory>();
        var model = factory.Create(connection, new DatabaseModelFactoryOptions());

        Assert.Contains(model.Sequences, s => s.Name == "DraftKeys");
    }

    private sealed class Draft
    {
        public long Id { get; set; }
    }

    private sealed class DraftContext(DbContextOptions<DraftContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Draft>().Property(d => d.Id).UseHiLo("DraftKeys");
        }
    }

    [Fact]
    public void Only_the_named_table_is_read_back()
    {
        using var database = ForumDatabase.Filled(nameof(Only_the_named_table_is_read_back));
        using var context = database.Context();
        var connection = context.Database.GetDbConnection();

        // A list of the wanted tables asks for those tables and only those: the keys and the
        // indexes of the rest are in the catalogue too, and none of them belongs to a table
        // that was not asked for.
        var model = ReadSchema(connection, new DatabaseModelFactoryOptions(tables: ["Forums"]));

        Assert.Equal(["Forums"], model.Tables.Select(t => t.Name));

        var qualified = ReadSchema(connection, new DatabaseModelFactoryOptions(tables: ["PUBLIC.Forums"]));

        Assert.Equal(["Forums"], qualified.Tables.Select(t => t.Name));
    }

    [Fact]
    public void The_foreign_keys_of_a_table_are_read_back()
    {
        using var connection = new H2Connection("jdbc:h2:mem:scaffold_keys_" + Guid.NewGuid().ToString("N"));
        connection.Open();

        Execute(connection, "CREATE TABLE \"Parent\" (\"Id\" INTEGER PRIMARY KEY)");
        Execute(connection, "CREATE TABLE \"Child\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER)");
        Execute(
            connection,
            "ALTER TABLE \"Child\" ADD CONSTRAINT \"FK_Child_Parent\" FOREIGN KEY (\"ParentId\") "
            + "REFERENCES \"Parent\" (\"Id\") ON DELETE CASCADE");

        var model = ReadSchema(connection, new DatabaseModelFactoryOptions());
        var child = model.Tables.Single(t => t.Name == "Child");
        var foreignKey = child.ForeignKeys.Single();

        Assert.Equal("FK_Child_Parent", foreignKey.Name);
        Assert.Equal("Parent", foreignKey.PrincipalTable.Name);
        Assert.Equal(["ParentId"], foreignKey.Columns.Select(c => c.Name));
        Assert.Equal(ReferentialAction.Cascade, foreignKey.OnDelete);
    }

    [Fact]
    public void A_computed_column_is_read_back()
    {
        using var connection = new H2Connection("jdbc:h2:mem:scaffold_computed_" + Guid.NewGuid().ToString("N"));
        connection.Open();

        Execute(
            connection,
            "CREATE TABLE \"Computed\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" VARCHAR, "
            + "\"Upper\" VARCHAR GENERATED ALWAYS AS (UPPER(\"Name\")))");

        var model = ReadSchema(connection, new DatabaseModelFactoryOptions());
        var column = model.Tables.Single(t => t.Name == "Computed").Columns.Single(c => c.Name == "Upper");

        Assert.Equal(ValueGenerated.OnAddOrUpdate, column.ValueGenerated);
        Assert.NotNull(column.ComputedColumnSql);
    }

    private static void Execute(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static DatabaseModel ReadSchema(DbConnection connection, DatabaseModelFactoryOptions options)
    {
        var services = new ServiceCollection().AddEntityFrameworkH2();

        // The services of the time of the design are what hold the reader of the catalogue.
        new H2DesignTimeServices().ConfigureDesignTimeServices(services);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDatabaseModelFactory>();

        return factory.Create(connection, options);
    }

    /// <summary>
    ///     Reads the schema of the database the context is connected to.
    /// </summary>
    /// <param name="context">The context over the database.</param>
    /// <returns>The model of the database.</returns>
    private static DatabaseModel Read(ForumContext context)
        => ReadSchema(context.Database.GetDbConnection(), new DatabaseModelFactoryOptions());
}
