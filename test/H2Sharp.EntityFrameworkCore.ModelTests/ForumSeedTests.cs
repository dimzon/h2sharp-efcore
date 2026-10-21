using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The forum is in the database: the tables are made and the rows are put in.
/// </summary>
public class ForumSeedTests
{
    [Fact]
    public void The_forum_is_in_the_database()
    {
        using var database = ForumDatabase.Filled(nameof(The_forum_is_in_the_database));
        using var context = database.Context();

        Assert.Equal(3, context.Forums.Count());
        Assert.Equal(5, context.Users.Count());
        Assert.Equal(7, context.Topics.Count());
        Assert.Equal(14, context.Posts.Count());
        Assert.Equal(4, context.Tags.Count());
        Assert.Equal(7, context.TopicTags.Count());
        Assert.Equal(5, context.Votes.Count());
    }

    [Fact]
    public void The_rows_keep_what_the_seed_put_in_them()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_keep_what_the_seed_put_in_them));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();

        Assert.Equal("What a compiler does with the source", topic.Title);
        Assert.Equal(ForumData.Start.AddDays(10), topic.CreatedAt);
        Assert.Equal(1200, topic.ViewCount);
    }

    [Fact]
    public void The_key_of_a_row_comes_back_from_the_database()
    {
        using var database = ForumDatabase.Filled(nameof(The_key_of_a_row_comes_back_from_the_database));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();

        Assert.True(topic.Id > 0);
    }
}
