using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for in the ways of a set: distinct, united, and so on.
/// </summary>
public class ForumSetQueryTests
{
    [Fact]
    public void The_rows_without_their_repeats_are_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_without_their_repeats_are_asked_for));
        using var context = database.Context();

        var authors = context.Posts.Select(p => p.AuthorId).Distinct().ToList();

        Assert.Equal(4, authors.Count);
        Assert.Contains(1, authors);
        Assert.Contains(4, authors);
    }

    [Fact]
    public void The_rows_of_two_queries_are_united()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_of_two_queries_are_united));
        using var context = database.Context();

        var topics = context.Topics
            .Where(t => t.ForumId == 1)
            .Union(context.Topics.Where(t => t.ForumId == 2))
            .ToList();

        Assert.Equal(6, topics.Count);
        Assert.Contains(topics, t => t.Title == "What a compiler does with the source");
        Assert.Contains(topics, t => t.Title == "Reading a query plan");
    }

    [Fact]
    public void The_rows_of_two_queries_are_put_together()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_of_two_queries_are_put_together));
        using var context = database.Context();

        var topics = context.Topics
            .Where(t => t.Id == 1)
            .Concat(context.Topics.Where(t => t.Id == 2))
            .ToList();

        Assert.Equal(2, topics.Count);
    }

    [Fact]
    public void Pages_are_read_without_a_limit_and_united_with_the_first_page()
    {
        using var database = ForumDatabase.Filled(nameof(Pages_are_read_without_a_limit_and_united_with_the_first_page));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // H2 2.5 reads OFFSET without LIMIT, and an operand of UNION that carries its
        // own ORDER BY or LIMIT is written in parentheses.
        Assert.Equal(
            source.Skip(2).Select(t => t.Title),
            context.Topics.OrderBy(t => t.Id).Skip(2).Select(t => t.Title).ToList());

        var first = context.Topics.OrderBy(t => t.Id).Take(2).Select(t => t.Title);
        var rest = context.Topics.OrderBy(t => t.Id).Skip(2).Select(t => t.Title);
        Assert.Equal(
            source.Take(2).Select(t => t.Title).Concat(source.Skip(2).Select(t => t.Title)),
            first.Concat(rest).ToList());
    }

    [Fact]
    public void The_authors_in_both_topics_are_met_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(The_authors_in_both_topics_are_met_on_reference_h2));
        using var context = database.Context();

        // INTERSECT and EXCEPT below are checked on the reference H2 instead: the engine
        // does not read set operations other than UNION yet.
        var met = context.Posts.Where(p => p.TopicId == 1).Select(p => p.AuthorId)
            .Intersect(context.Posts.Where(p => p.TopicId == 2).Select(p => p.AuthorId))
            .OrderBy(a => a);
        Assert.Equal(["1", "2"], ReferenceForum.Query(context, met).Select(row => row[0]));

        var apart = context.Posts.Where(p => p.TopicId == 1).Select(p => p.AuthorId)
            .Except(context.Posts.Where(p => p.TopicId == 2).Select(p => p.AuthorId))
            .OrderBy(a => a);
        Assert.Equal(["3", "4"], ReferenceForum.Query(context, apart).Select(row => row[0]));
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_rows_of_two_queries_are_met()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_of_two_queries_are_met));
        using var context = database.Context();

        // The engine does not read INTERSECT: "Not ported yet: Parser.parseQuerySelect: INTERSECT
        // is not ported".
        var authors = context.Posts
            .Where(p => p.TopicId == 1)
            .Intersect(context.Posts.Where(p => p.TopicId == 2))
            .Select(p => p.AuthorId)
            .ToList();

        Assert.Equal([2], authors);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_rows_of_two_queries_are_set_apart()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_of_two_queries_are_set_apart));
        using var context = database.Context();

        // The engine does not read EXCEPT: "Not ported yet: Parser.parseQuerySelect: EXCEPT is not
        // ported".
        var authors = context.Posts
            .Where(p => p.TopicId == 1)
            .Except(context.Posts.Where(p => p.TopicId == 2))
            .Select(p => p.AuthorId)
            .ToList();

        Assert.Equal([1, 3, 4], authors);
    }

    [Fact]
    public void The_rows_without_their_repeats_are_asked_for_in_order()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_without_their_repeats_are_asked_for_in_order));
        using var context = database.Context();

        var authors = context.Posts.Select(p => p.AuthorId).Distinct().OrderBy(a => a).ToList();

        Assert.Equal([1, 2, 3, 4], authors);
    }

    [Fact]
    public void The_rows_are_asked_for_in_the_reverse_order()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_in_the_reverse_order));
        using var context = database.Context();

        var titles = context.Topics
            .OrderBy(t => t.Id)
            .Reverse()
            .Select(t => t.Title)
            .ToList();

        Assert.Equal("The forum moves", titles[0]);
    }

    [Fact]
    public void The_rows_are_grouped_and_filtered_after_the_grouping()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_grouped_and_filtered_after_the_grouping));
        using var context = database.Context();

        var groups = context.Posts
            .GroupBy(p => p.TopicId)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        Assert.Equal([1, 2, 4, 5, 6], groups);
    }

    [Fact]
    public void The_rows_are_asked_for_by_their_place()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_by_their_place));
        using var context = database.Context();

        var title = context.Topics.OrderBy(t => t.Id).Skip(3).First().Title;

        Assert.Equal("Reading a query plan", title);
    }

    [Fact]
    public void The_last_row_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_last_row_is_asked_for));
        using var context = database.Context();

        var title = context.Topics.OrderBy(t => t.Id).Last().Title;

        Assert.Equal("The forum moves", title);
    }

    [Fact]
    public void The_only_row_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_only_row_is_asked_for));
        using var context = database.Context();

        var title = context.Topics.Single(t => t.Id == 3).Title;

        Assert.Equal("The cost of an abstraction", title);
    }

    [Fact]
    public void The_rows_are_asked_for_with_their_count_in_the_query()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_their_count_in_the_query));
        using var context = database.Context();

        var rows = context.Posts
            .GroupBy(p => p.TopicId)
            .OrderBy(g => g.Key)
            .Select(g => new { TopicId = g.Key, Count = g.Count(), First = g.Min(p => p.Id) })
            .ToList();

        Assert.Equal(7, rows.Count);
        Assert.Equal(4, rows[0].Count);
        Assert.Equal(1, rows[0].First);
    }
}
