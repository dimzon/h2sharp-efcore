using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows go into the database.
/// </summary>
public class ForumInsertTests
{
    [Fact]
    public void One_row_goes_in()
    {
        using var database = ForumDatabase.Empty(nameof(One_row_goes_in));
        using var context = database.Context();
        context.Database.EnsureCreated();

        context.Forums.Add(new Forum { Name = "One", Description = "The first forum." });

        context.SaveChanges();

        Assert.Equal(1, context.Forums.Count());
    }

    [Fact]
    public void Two_rows_go_in()
    {
        using var database = ForumDatabase.Empty(nameof(Two_rows_go_in));
        using var context = database.Context();
        context.Database.EnsureCreated();

        context.Forums.Add(new Forum { Name = "One", Description = "The first forum." });
        context.Forums.Add(new Forum { Name = "Two", Description = "The second forum." });

        context.SaveChanges();

        Assert.Equal(2, context.Forums.Count());
    }

    [Fact]
    public void Many_rows_go_in()
    {
        using var database = ForumDatabase.Empty(nameof(Many_rows_go_in));
        using var context = database.Context();
        context.Database.EnsureCreated();

        context.Forums.AddRange(ForumData.Forums());

        context.SaveChanges();

        Assert.Equal(3, context.Forums.Count());
    }
}
