using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for in the ways of SQL: cases, nulls, comparisons,
///     arithmetic, and the rest.
/// </summary>
public class ForumSqlQueryTests
{
    [Fact]
    public void The_rows_are_asked_for_with_a_case()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_case));
        using var context = database.Context();

        var titles = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => t.ViewCount > 500 ? "many" : "few")
            .ToList();

        Assert.Equal("many", titles[0]);
        Assert.Equal("few", titles[2]);
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_empty_column_taken_of_the_other()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_empty_column_taken_of_the_other));
        using var context = database.Context();

        var notes = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => t.ModeratorNote ?? "no note")
            .ToList();

        Assert.Equal("no note", notes[0]);
        Assert.Equal("Locked: the thread turned into a flame war.", notes[2]);
    }

    [Fact]
    public void The_rows_are_asked_for_with_an_empty_column_compared()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_an_empty_column_compared));
        using var context = database.Context();

        Assert.Equal(5, context.Topics.Count(t => t.ModeratorNote == null));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_empty_column_of_a_person()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_empty_column_of_a_person));
        using var context = database.Context();

        Assert.Equal(2, context.Users.Count(u => u.BirthDate == null));
    }

    [Fact]
    public void The_rows_are_asked_for_with_two_conditions_joined()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_two_conditions_joined));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => t.ViewCount > 200 && t.IsPinned == false && t.ForumId == 1));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_condition_on_two_tables()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_condition_on_two_tables));
        using var context = database.Context();

        var titles = context.Topics
            .Join(
                context.Forums,
                topic => topic.ForumId,
                forum => forum.Id,
                (topic, forum) => new { topic.Title, forum.Name })
            .Where(row => row.Name == "Databases")
            .OrderBy(row => row.Title)
            .Select(row => row.Title)
            .ToList();

        Assert.Equal(3, titles.Count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_table_joined_from_the_left()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_table_joined_from_the_left));
        using var context = database.Context();

        // The join from the left becomes a query in the list of the columns,
        // "( SELECT count(*) FROM "TopicTags" "t0" WHERE "t"."Id" = "t0"."TopicId" )", and the
        // port reads a scalar query there (dimzon/h2sharp#37).
        var rows = context.Topics
            .GroupJoin(
                context.TopicTags,
                topic => topic.Id,
                topicTag => topicTag.TopicId,
                (topic, tags) => new { topic.Title, Count = tags.Count() })
            .OrderBy(row => row.Title)
            .ToList();

        Assert.Equal(7, rows.Count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_number_of_the_rows_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_number_of_the_rows_asked_for));
        using var context = database.Context();

        Assert.Equal(7, context.Topics.Count());
        Assert.Equal(1, context.Topics.Count(t => t.Title == "The forum moves"));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_sum_and_the_average_and_the_extremes()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_sum_and_the_average_and_the_extremes));
        using var context = database.Context();

        Assert.Equal(3680, context.Topics.Sum(t => t.ViewCount));
        Assert.Equal(525.71, context.Topics.Average(t => t.ViewCount), 2);
        Assert.Equal(80, context.Topics.Min(t => t.ViewCount));
        Assert.Equal(1200, context.Topics.Max(t => t.ViewCount));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_sum_of_a_decimal()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_sum_of_a_decimal));
        using var context = database.Context();

        Assert.Equal(23.5m, context.Users.Sum(u => u.Balance));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_number_of_the_rows_of_a_group()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_number_of_the_rows_of_a_group));
        using var context = database.Context();

        Assert.Equal(4, context.Posts.Count(p => p.TopicId == 1));
        Assert.Equal(1, context.Posts.Count(p => p.TopicId == 3));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_order_of_two_columns()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_order_of_two_columns));
        using var context = database.Context();

        var titles = context.Posts
            .OrderBy(p => p.TopicId)
            .ThenByDescending(p => p.Id)
            .Select(p => p.Id)
            .Take(4)
            .ToList();

        Assert.Equal([4, 3, 2, 1], titles);
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_order_of_a_column_that_is_not_the_first()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_order_of_a_column_that_is_not_the_first));
        using var context = database.Context();

        var titles = context.Topics
            .OrderByDescending(t => t.ViewCount)
            .Select(t => t.Title)
            .First();

        Assert.Equal("What a compiler does with the source", titles);
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_number_of_the_rows_asked_for_through_a_number()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_number_of_the_rows_asked_for_through_a_number));
        using var context = database.Context();

        Assert.Equal(2, context.Forums.Count(f => f.IsArchived == false));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_number_of_the_day()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_number_of_the_day));
        using var context = database.Context();

        Assert.Equal(2, context.Posts.Count(p => p.CreatedAt.Date.Day == 11));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_date_moved_by_a_unit()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_date_moved_by_a_unit));
        using var context = database.Context();

        // The provider writes TIMESTAMPADD, the form H2 2.5 has, and the engine reads the
        // function with a number written out: the amount here is a parameter, and the engine
        // answers "Value.getLong needs the concrete value type for NUMERIC, which is not ported
        // yet".
        var titles = context.Topics
            .Where(t => t.CreatedAt.AddDays(1) > ForumData.Start)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(7, titles.Count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_date_moved_by_a_year()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_date_moved_by_a_year));
        using var context = database.Context();

        // The engine reads the add and the member read off it; the answer is the count
        // of the same predicate over the seed, not a number written by hand.
        Assert.Equal(
            ForumData.Topics().Count(t => t.CreatedAt.AddYears(1).Year == 2025),
            context.Topics.Count(t => t.CreatedAt.AddYears(1).Year == 2025));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_date_moved_by_hours()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_date_moved_by_hours));
        using var context = database.Context();

        Assert.Equal(
            ForumData.Posts().Count(p => p.CreatedAt.AddHours(1).Hour == 11),
            context.Posts.Count(p => p.CreatedAt.AddHours(1).Hour == 11));
    }
}
