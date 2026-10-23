using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

public class ForumCursorAndBulkExamples
{
    [Fact]
    public void Composite_cursor_runs_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(Composite_cursor_runs_on_reference_h2));
        using var context = database.Context();
        var query = context.Topics.Where(t => EF.Functions.GreaterThan(
                new ValueTuple<DateTime, int>(t.CreatedAt, t.Id), new ValueTuple<DateTime, int>(ForumData.Start.AddDays(10), 1)))
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).Take(3).Select(t => t.Title);
        Assert.Equal(new[] { "Reading a query plan", "Types without a runtime", "Transactions and the order of the writes" },
            ReferenceForum.Query(context, query).Select(row => row[0]));
    }

    [Fact]
    public void Composite_cursor_reads_the_next_page_without_offset()
    {
        using var database = ForumDatabase.Filled(nameof(Composite_cursor_reads_the_next_page_without_offset));
        using var context = database.Context();
        var cursor = context.Topics.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).Select(t => new { t.CreatedAt, t.Id }).First();
        var page = context.Topics
            .Where(t => EF.Functions.GreaterThan(new ValueTuple<DateTime, int>(t.CreatedAt, t.Id),
                new ValueTuple<DateTime, int>(cursor.CreatedAt, cursor.Id)))
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).Take(3).Select(t => t.Title).ToList();
        Assert.Equal(new[] { "Reading a query plan", "Types without a runtime", "Transactions and the order of the writes" }, page);

        var previous = context.Topics.Where(t => EF.Functions.LessThanOrEqual(
            new ValueTuple<DateTime, int>(t.CreatedAt, t.Id), new ValueTuple<DateTime, int>(cursor.CreatedAt, cursor.Id)))
            .Select(t => t.Title).ToList();
        Assert.Equal(new[] { "What a compiler does with the source" }, previous);
    }

    [Fact]
    public async Task Moderator_closes_old_topics_without_loading_them()
    {
        using var database = ForumDatabase.Filled(nameof(Moderator_closes_old_topics_without_loading_them));
        using var context = database.Context();
        context.ChangeTracker.Clear();
        var cutoff = ForumData.Now.AddDays(-5);
        var changed = await context.Topics.Where(t => t.LastPostAt < cutoff && !t.IsClosed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.IsClosed, true).SetProperty(t => t.ModeratorNote, "Inactive"));
        Assert.Equal(1, changed);
        Assert.Empty(context.ChangeTracker.Entries());
        var topic = context.Topics.Single(t => t.Title == "Indexes that are never used");
        Assert.True(topic.IsClosed);
        Assert.Equal("Inactive", topic.ModeratorNote);
    }

    [Fact]
    public void Hidden_posts_are_deleted_without_loading_them()
    {
        using var database = ForumDatabase.Filled(nameof(Hidden_posts_are_deleted_without_loading_them));
        using var context = database.Context();
        Assert.Equal(1, context.Posts.Where(p => p.IsHidden).ExecuteDelete());
        Assert.Equal(13, context.Posts.Count());
        Assert.Equal(0, context.Posts.Count(p => p.IsHidden));
    }

    [Fact]
    public void View_counts_are_updated_with_a_database_expression()
    {
        using var database = ForumDatabase.Filled(nameof(View_counts_are_updated_with_a_database_expression));
        using var context = database.Context();
        Assert.Equal(3, context.Topics.Where(t => t.ForumId == 1)
            .ExecuteUpdate(setters => setters.SetProperty(t => t.ViewCount, t => t.ViewCount + 1)));
        Assert.Equal(new[] { 1201, 641, 301 }, context.Topics.Where(t => t.ForumId == 1).OrderBy(t => t.Id).Select(t => t.ViewCount).ToList());
    }

    [Fact]
    public void View_count_update_keeps_each_rows_own_value_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(View_count_update_keeps_each_rows_own_value_on_reference_h2));
        var recorder = new SqlRecorder();
        using var original = database.Context();
        using var context = new Model.ForumContext(new DbContextOptionsBuilder<Model.ForumContext>()
            .UseH2(original.Database.GetDbConnection()).AddInterceptors(recorder).Options);
        _ = Record.Exception(() => context.Topics.Where(t => t.ForumId == 1)
            .ExecuteUpdate(setters => setters.SetProperty(t => t.ViewCount, t => t.ViewCount + 1)));
        var command = recorder.Commands.Single(sql => sql.StartsWith("UPDATE", StringComparison.Ordinal));
        var rows = H2ReferenceOracle.Query(ReferenceForum.Setup(context) + command + ";",
            "SELECT \"ViewCount\" FROM \"Topics\" WHERE \"ForumId\" = 1 ORDER BY \"Id\"");
        Assert.Equal(new[] { "1201", "641", "301" }, rows.Select(row => row[0]));
    }
}
