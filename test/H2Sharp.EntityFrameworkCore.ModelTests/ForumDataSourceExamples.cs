using H2Sharp.EntityFrameworkCore.ModelTests.Model;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The database belongs to a data source: the data source creates it, keeps it alive and hands
///     out the connections to it. One database has one data source, and every connection to that
///     database comes from it.
/// </summary>
public class ForumDataSourceExamples
{
    [Fact]
    public void Two_contexts_over_one_data_source_work_with_one_database()
    {
        using var dataSource = new H2DataSourceBuilder(
            $"Data Source=forum_datasource_{Guid.NewGuid():N};Storage=Memory").Build();

        using var first = new ForumContext(new DbContextOptionsBuilder<ForumContext>().UseH2(dataSource).Options);
        using var second = new ForumContext(new DbContextOptionsBuilder<ForumContext>().UseH2(dataSource).Options);

        first.Database.EnsureCreated();
        first.Forums.Add(new Forum { Name = "Data sources", Description = "One database, one data source" });
        first.SaveChanges();

        // The second context has a connection of its own, and that connection is a connection to
        // the same database: the data source owns the database and hands out the connections to it.
        Assert.Equal("Data sources", second.Forums.Single().Name);
    }

    [Fact]
    public void Two_contexts_over_one_connection_string_work_with_one_database()
    {
        var connectionString = $"Data Source=forum_datasource_shared_{Guid.NewGuid():N};Storage=Memory";

        using var first = new ForumContext(new DbContextOptionsBuilder<ForumContext>().UseH2(connectionString).Options);
        using var second = new ForumContext(new DbContextOptionsBuilder<ForumContext>().UseH2(connectionString).Options);

        first.Database.EnsureCreated();
        first.Forums.Add(new Forum { Name = "Shared", Description = "The connection string names the database" });
        first.SaveChanges();

        // The provider keeps one data source per database, so the connection string names the
        // database and both contexts work with it.
        Assert.Equal("Shared", second.Forums.Single().Name);
    }
}
