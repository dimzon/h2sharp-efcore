using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for without waiting.
/// </summary>
/// <remarks>
///     The provider has a path of its own for every asynchronous call, and the paths are
///     different code, so they are checked here too.
/// </remarks>
public class ForumAsyncTests
{
    [Fact]
    public async Task The_rows_are_asked_for_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_without_waiting));
        await using var context = database.Context();

        List<string> titles = await context.Topics.OrderBy(t => t.Id).Select(t => t.Title).ToListAsync();

        Assert.Equal(7, titles.Count);
        Assert.Equal("What a compiler does with the source", titles[0]);
    }

    [Fact]
    public async Task The_number_of_the_rows_is_asked_for_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(The_number_of_the_rows_is_asked_for_without_waiting));
        await using var context = database.Context();

        int count = await context.Posts.CountAsync();

        Assert.Equal(14, count);
    }

    [Fact]
    public async Task The_first_row_is_asked_for_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(The_first_row_is_asked_for_without_waiting));
        await using var context = database.Context();

        var topic = await context.Topics.OrderBy(t => t.Id).FirstAsync();

        Assert.Equal("What a compiler does with the source", topic.Title);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public async Task It_is_asked_whether_there_is_a_row_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(It_is_asked_whether_there_is_a_row_without_waiting));
        await using var context = database.Context();

        // The provider writes EXISTS, and the engine does not read it yet.
        Assert.False(await context.Topics.AnyAsync(t => t.ViewCount > 5000));
    }

    [Fact]
    public async Task A_row_is_looked_up_by_its_key_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_looked_up_by_its_key_without_waiting));
        await using var context = database.Context();

        int id = await context.Topics.OrderBy(t => t.Id).Select(t => t.Id).FirstAsync();
        var topic = await context.Topics.FindAsync(id);

        Assert.NotNull(topic);
        Assert.Equal("What a compiler does with the source", topic.Title);
    }

    [Fact]
    public async Task A_row_goes_in_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_goes_in_without_waiting));
        await using var context = database.Context();

        context.Tags.Add(new Tag { Name = "async" });

        await context.SaveChangesAsync();

        Assert.Equal(5, await context.Tags.CountAsync());
    }

    [Fact]
    public async Task A_row_is_changed_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_changed_without_waiting));
        await using var context = database.Context();

        var topic = await context.Topics.OrderBy(t => t.Id).FirstAsync();
        topic.Title = "A title of an asynchronous change";
        await context.SaveChangesAsync();

        Assert.Equal("A title of an asynchronous change", (await context.Topics.OrderBy(t => t.Id).FirstAsync()).Title);
    }

    [Fact]
    public async Task A_row_is_removed_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_removed_without_waiting));
        await using var context = database.Context();

        var topic = await context.Topics.OrderBy(t => t.Id).FirstAsync();
        context.Topics.Remove(topic);
        await context.SaveChangesAsync();

        Assert.Equal(6, await context.Topics.CountAsync());
    }

    [Fact]
    public async Task A_row_is_removed_by_a_query_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_removed_by_a_query_without_waiting));
        await using var context = database.Context();

        int removed = await context.Posts.Where(p => p.IsHidden).ExecuteDeleteAsync();

        Assert.Equal(1, removed);
        Assert.Equal(13, await context.Posts.CountAsync());
    }

    [Fact]
    public async Task A_row_is_changed_by_a_query_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_changed_by_a_query_without_waiting));
        await using var context = database.Context();

        int changed = await context.Topics.Where(t => t.ForumId == 1).ExecuteUpdateAsync(s => s.SetProperty(t => t.ViewCount, 1));

        Assert.Equal(3, changed);
        Assert.Equal(3, await context.Topics.CountAsync(t => t.ViewCount == 1));
    }

    [Fact]
    public async Task The_rows_are_asked_for_with_a_raw_statement_without_waiting()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_raw_statement_without_waiting));
        await using var context = database.Context();

        var topics = await context.Topics
            .FromSqlRaw("SELECT * FROM \"Topics\" ORDER BY \"Id\"")
            .ToListAsync();

        Assert.Equal(7, topics.Count);
    }
}
