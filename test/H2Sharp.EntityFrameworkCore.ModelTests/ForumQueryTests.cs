using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for.
/// </summary>
/// <remarks>
///     <para>
///         Every test here starts from the same filled database, so that the answer of the
///         query is the same every time and can be written down.
///     </para>
///     <para>
///         The tests ask the questions an application asks: which topics are the newest, who
///         wrote the most, which forums are quiet, and so on.
///     </para>
///     <para>
///         The forms the engine does not read yet are in <see cref="ForumPortGapTests" />, where
///         they are marked as the gaps of the engine they are.
///     </para>
/// </remarks>
public class ForumQueryTests
{
    [Fact]
    public void All_the_topics_come_back()
    {
        using var database = ForumDatabase.Filled(nameof(All_the_topics_come_back));
        using var context = database.Context();

        List<string> titles = context.Topics.OrderBy(t => t.Id).Select(t => t.Title).ToList();

        Assert.Equal(7, titles.Count);
        Assert.Equal("What a compiler does with the source", titles[0]);
        Assert.Equal("The forum moves", titles[6]);
    }

    [Fact]
    public void A_row_is_asked_for_by_its_key()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_asked_for_by_its_key));
        using var context = database.Context();

        int id = context.Topics.OrderBy(t => t.Id).Select(t => t.Id).First();
        var topic = context.Topics.Single(t => t.Id == id);

        Assert.Equal("What a compiler does with the source", topic.Title);
    }

    [Fact]
    public void The_rows_of_one_forum_are_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_of_one_forum_are_asked_for));
        using var context = database.Context();

        int forumId = context.Forums.Single(f => f.Name == "Databases").Id;

        List<string> titles = context.Topics
            .Where(t => t.ForumId == forumId)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "Reading a query plan",
                "Indexes that are never used",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_topics_are_asked_for_by_their_name()
    {
        using var database = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_their_name));
        using var context = database.Context();

        List<string> titles = context.Topics
            .Where(t => t.Title.Contains("compiler"))
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(["What a compiler does with the source"], titles);
    }

    [Fact]
    public void The_topics_are_asked_for_by_the_beginning_of_their_name()
    {
        using var database = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_the_beginning_of_their_name));
        using var context = database.Context();

        List<string> titles = context.Topics
            .Where(t => t.Title.StartsWith("The"))
            .OrderBy(t => t.Title)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "The cost of an abstraction",
                "The forum moves",
            ],
            titles);
    }

    [Fact]
    public void The_topics_are_asked_for_by_the_end_of_their_name()
    {
        using var database = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_the_end_of_their_name));
        using var context = database.Context();

        List<string> titles = context.Topics
            .Where(t => t.Title.EndsWith("plan"))
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(["Reading a query plan"], titles);
    }

    [Fact]
    public void The_topics_are_asked_for_in_the_order_of_their_last_post()
    {
        using var database = ForumDatabase.Filled(nameof(The_topics_are_asked_for_in_the_order_of_their_last_post));
        using var context = database.Context();

        List<string> titles = context.Topics
            .OrderByDescending(t => t.LastPostAt)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal("What a compiler does with the source", titles[0]);
        Assert.Equal("The forum moves", titles[6]);
    }

    [Fact]
    public void The_topics_are_asked_for_in_the_order_of_their_views()
    {
        using var database = ForumDatabase.Filled(nameof(The_topics_are_asked_for_in_the_order_of_their_views));
        using var context = database.Context();

        List<string> titles = context.Topics
            .OrderByDescending(t => t.ViewCount)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal("What a compiler does with the source", titles[0]);
        Assert.Equal("The forum moves", titles[6]);
    }

    [Fact]
    public void A_page_of_the_topics_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(A_page_of_the_topics_is_asked_for));
        using var context = database.Context();

        List<string> titles = context.Topics
            .OrderBy(t => t.Id)
            .Skip(2)
            .Take(3)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "The cost of an abstraction",
                "Reading a query plan",
                "Indexes that are never used",
            ],
            titles);
    }

    [Fact]
    public void The_number_of_the_rows_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_number_of_the_rows_is_asked_for));
        using var context = database.Context();

        Assert.Equal(14, context.Posts.Count());
        Assert.Equal(1, context.Posts.Count(p => p.IsHidden));
        Assert.Equal(13, context.Posts.Count(p => !p.IsHidden));
    }

    [Fact]
    public void The_number_of_the_rows_of_one_forum_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_number_of_the_rows_of_one_forum_is_asked_for));
        using var context = database.Context();

        int forumId = context.Forums.Single(f => f.Name == "Programming").Id;

        Assert.Equal(3, context.Topics.Count(t => t.ForumId == forumId));
    }

    [Fact]
    public void The_sum_of_a_column_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_sum_of_a_column_is_asked_for));
        using var context = database.Context();

        int total = context.Topics.Sum(t => t.ViewCount);

        Assert.Equal(3680, total);
    }

    [Fact]
    public void The_average_of_a_column_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_average_of_a_column_is_asked_for));
        using var context = database.Context();

        double average = context.Topics.Average(t => t.ViewCount);

        Assert.Equal(525.71, average, 2);
    }

    [Fact]
    public void The_smallest_and_the_largest_of_a_column_are_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_smallest_and_the_largest_of_a_column_are_asked_for));
        using var context = database.Context();

        Assert.Equal(80, context.Topics.Min(t => t.ViewCount));
        Assert.Equal(1200, context.Topics.Max(t => t.ViewCount));
    }

    [Fact]
    public void The_rows_are_grouped_and_counted()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_grouped_and_counted));
        using var context = database.Context();

        var counts = context.Posts
            .GroupBy(p => p.TopicId)
            .OrderBy(g => g.Key)
            .Select(g => new { TopicId = g.Key, Count = g.Count() })
            .ToList();

        Assert.Equal(7, counts.Count);
        Assert.Equal(4, counts[0].Count);
        Assert.Equal(1, counts[6].Count);
    }

    [Fact]
    public void The_rows_are_grouped_and_summed()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_grouped_and_summed));
        using var context = database.Context();

        var totals = context.Posts
            .GroupBy(p => p.TopicId)
            .OrderBy(g => g.Key)
            .Select(g => g.Sum(p => p.Id))
            .ToList();

        Assert.Equal([1 + 2 + 3 + 4, 5 + 6, 7, 8 + 9, 10 + 11, 12 + 13, 14], totals);
    }

    [Fact]
    public void Two_tables_are_joined()
    {
        using var database = ForumDatabase.Filled(nameof(Two_tables_are_joined));
        using var context = database.Context();

        var rows = context.Topics
            .Join(
                context.Forums,
                topic => topic.ForumId,
                forum => forum.Id,
                (topic, forum) => new { topic.Title, ForumName = forum.Name })
            .OrderBy(row => row.Title)
            .ToList();

        Assert.Equal(7, rows.Count);
        Assert.Equal("Databases", rows.Single(r => r.Title == "Reading a query plan").ForumName);
    }

    [Fact]
    public void A_table_is_joined_with_a_group_by()
    {
        using var database = ForumDatabase.Filled(nameof(A_table_is_joined_with_a_group_by));
        using var context = database.Context();

        var rows = context.Forums
            .Join(
                context.Topics,
                forum => forum.Id,
                topic => topic.ForumId,
                (forum, topic) => new { forum.Name, topic.ViewCount })
            .GroupBy(row => row.Name)
            .OrderBy(group => group.Key)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(3, rows.Single(r => r.Name == "Programming").Count);
        Assert.Equal(1, rows.Single(r => r.Name == "Old announcements").Count);
    }

    [Fact]
    public void A_table_is_joined_with_a_row_of_another_table()
    {
        using var database = ForumDatabase.Filled(nameof(A_table_is_joined_with_a_row_of_another_table));
        using var context = database.Context();

        int forumId = context.Forums.Single(f => f.Name == "Programming").Id;

        var rows = context.Topics
            .Where(topic => topic.ForumId == forumId)
            .Join(
                context.Users,
                topic => topic.AuthorId,
                user => user.Id,
                (topic, user) => new { topic.Title, user.UserName })
            .OrderBy(row => row.Title)
            .ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal("ada", rows.Single(r => r.Title == "What a compiler does with the source").UserName);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_condition_over_two_columns()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_condition_over_two_columns));
        using var context = database.Context();

        List<string> titles = context.Topics
            .Where(t => t.ViewCount > 200 && t.ViewCount < 700)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "Types without a runtime",
                "The cost of an abstraction",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_condition_over_a_date()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_condition_over_a_date));
        using var context = database.Context();

        DateTime begin = ForumData.Start.AddDays(15);
        DateTime end = ForumData.Start.AddDays(45);

        List<string> titles = context.Topics
            .Where(t => t.CreatedAt >= begin && t.CreatedAt < end)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "Types without a runtime",
                "The cost of an abstraction",
                "Reading a query plan",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_null_column()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_null_column));
        using var context = database.Context();

        Assert.Equal(2, context.Users.Count(p => p.BirthDate == null));
    }

    [Fact]
    public void Only_the_needed_columns_are_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(Only_the_needed_columns_are_asked_for));
        using var context = database.Context();

        var rows = context.Posts
            .OrderBy(p => p.Id)
            .Select(p => new { p.TopicId, p.AuthorId })
            .ToList();

        Assert.Equal(14, rows.Count);
        Assert.Equal(1, rows[0].TopicId);
        Assert.Equal(1, rows[0].AuthorId);
    }

    [Fact]
    public void The_rows_are_asked_for_without_being_tracked()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_without_being_tracked));
        using var context = database.Context();

        List<string> titles = context.Topics
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(7, titles.Count);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public void The_rows_are_asked_for_as_a_single_value()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_as_a_single_value));
        using var context = database.Context();

        Assert.Equal("What a compiler does with the source", context.Topics.OrderBy(t => t.Id).First().Title);
        Assert.Equal("The forum moves", context.Topics.OrderByDescending(t => t.Id).First().Title);
        Assert.Equal(14, context.Posts.Count());
    }

    [Fact]
    public void The_rows_of_one_table_are_asked_for_twice()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_of_one_table_are_asked_for_twice));
        using var context = database.Context();

        int visible = context.Posts.Count(p => !p.IsHidden);
        int hidden = context.Posts.Count(p => p.IsHidden);

        Assert.Equal(14, visible + hidden);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void It_is_asked_whether_there_is_a_row()
    {
        using var database = ForumDatabase.Filled(nameof(It_is_asked_whether_there_is_a_row));
        using var context = database.Context();

        // The provider writes EXISTS, and the engine does not read it yet:
        // "Not ported yet: Parser.readTerm: the keyword function EXISTS is not ported".
        Assert.True(context.Topics.Any(t => t.ViewCount > 1000));
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void It_is_asked_whether_every_row_holds_a_condition()
    {
        using var database = ForumDatabase.Filled(nameof(It_is_asked_whether_every_row_holds_a_condition));
        using var context = database.Context();

        // The provider writes NOT EXISTS, and the engine does not read it yet.
        Assert.True(context.Users.All(u => u.UserName.Length > 0));
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_rows_are_asked_for_with_a_list_in_a_variable()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_list_in_a_variable));
        using var context = database.Context();

        // The provider writes "= ANY (@__ids_0)", and the engine does not read the form:
        // "Not ported yet: Parser.parseQuerySelect: aggregate calls outside the select list are
        // not ported".
        int[] ids = [1, 2];
        Assert.Equal(6, context.Topics.Count(t => ids.Contains(t.ForumId)));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_list()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_list));
        using var context = database.Context();

        int count = context.Topics.Count(t => new[] { 1, 2 }.Contains(t.ForumId));

        Assert.Equal(6, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_like()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_like));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => EF.Functions.Like(t.Title, "The%")));
    }

    [Fact]
    public void The_rows_are_asked_for_with_an_ilike()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_an_ilike));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => H2DbFunctionsExtensions.ILike(null!, t.Title, "the%")));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_year_of_a_date()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_year_of_a_date));
        using var context = database.Context();

        Assert.Equal(7, context.Topics.Count(t => t.CreatedAt.Year == 2024));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_day_of_a_date()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_day_of_a_date));
        using var context = database.Context();

        Assert.Equal(1, context.Topics.Count(t => t.CreatedAt.Day == 10));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_length_of_a_string()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_length_of_a_string));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => t.Title.Length > 30));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_substring()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_substring));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => t.Title.Substring(0, 3) == "The"));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_concat()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_concat));
        using var context = database.Context();

        Assert.Equal(1, context.Topics.Count(t => t.Title == "The" + " forum moves"));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_role_of_a_person()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_role_of_a_person));
        using var context = database.Context();

        Assert.Equal(2, context.Users.Count(u => u.Role == UserRole.Moderator || u.Role == UserRole.Administrator));
        Assert.Equal(1, context.Users.Count(u => u.Role == UserRole.Guest));
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_banned_person()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_banned_person));
        using var context = database.Context();

        Assert.Equal(1, context.Users.Count(u => u.IsBanned));
    }
}
