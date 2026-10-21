using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The functions of H2 the provider offers to the model.
/// </summary>
/// <remarks>
///     The provider offers the built-ins of H2 that the engine can run. Every function here is
///     asked for in the way an application asks for it: in a query, over the rows of the forum.
/// </remarks>
public class ForumFunctionTests
{
    [Fact]
    public void The_date_is_moved_by_a_unit()
    {
        using var database = ForumDatabase.Filled(nameof(The_date_is_moved_by_a_unit));
        using var context = database.Context();

        var topics = context.Topics
            .Where(t => EF.Functions.DateAdd("DAY", 1, t.CreatedAt) > ForumData.Start)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(7, topics.Count);
    }

    [Fact]
    public void The_days_between_two_dates_are_counted()
    {
        using var database = ForumDatabase.Filled(nameof(The_days_between_two_dates_are_counted));
        using var context = database.Context();

        long days = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.DateDiff("DAY", ForumData.Start, t.CreatedAt))
            .First();

        Assert.Equal(10, days);
    }

    [Fact]
    public void A_part_of_a_date_is_taken()
    {
        using var database = ForumDatabase.Filled(nameof(A_part_of_a_date_is_taken));
        using var context = database.Context();

        int year = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.Extract("YEAR", t.CreatedAt))
            .First();

        Assert.Equal(2024, year);
    }

    [Fact]
    public void The_position_of_a_substring_is_found()
    {
        using var database = ForumDatabase.Filled(nameof(The_position_of_a_substring_is_found));
        using var context = database.Context();

        int position = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.Locate(t.Body, "compiler"))
            .First();

        Assert.Equal(0, position);
    }

    [Fact]
    public void A_string_is_padded_to_the_left()
    {
        using var database = ForumDatabase.Filled(nameof(A_string_is_padded_to_the_left));
        using var context = database.Context();

        var padded = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.LeftPad(t.Title, 40, "."))
            .First();

        Assert.Equal(40, padded.Length);
        Assert.StartsWith(".", padded, StringComparison.Ordinal);
    }

    [Fact]
    public void A_string_is_padded_to_the_right()
    {
        using var database = ForumDatabase.Filled(nameof(A_string_is_padded_to_the_right));
        using var context = database.Context();

        var padded = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.RightPad(t.Title, 40, "."))
            .First();

        Assert.Equal(40, padded.Length);
        Assert.EndsWith(".", padded, StringComparison.Ordinal);
    }

    [Fact]
    public void A_string_is_repeated()
    {
        using var database = ForumDatabase.Filled(nameof(A_string_is_repeated));
        using var context = database.Context();

        var repeated = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.Repeat("ab", 3))
            .First();

        Assert.Equal("ababab", repeated);
    }

    [Fact]
    public void The_characters_of_a_string_are_translated()
    {
        using var database = ForumDatabase.Filled(nameof(The_characters_of_a_string_are_translated));
        using var context = database.Context();

        var translated = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.Translate(t.Title, "What", "That"))
            .First();

        Assert.StartsWith("That", translated, StringComparison.Ordinal);
    }

    [Fact]
    public void A_string_of_spaces_is_made()
    {
        using var database = ForumDatabase.Filled(nameof(A_string_of_spaces_is_made));
        using var context = database.Context();

        var spaces = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.Space(4))
            .First();

        Assert.Equal("    ", spaces);
    }

    [Fact]
    public void The_bits_of_two_numbers_are_compared()
    {
        using var database = ForumDatabase.Filled(nameof(The_bits_of_two_numbers_are_compared));
        using var context = database.Context();

        // The lowest bit of every view count of the forum is zero: they are all even.
        int count = context.Topics.Count(t => EF.Functions.BitAnd(t.ViewCount, 1) == 0);

        Assert.Equal(7, count);
    }

    [Fact]
    public void The_code_of_the_first_character_is_taken()
    {
        using var database = ForumDatabase.Filled(nameof(The_code_of_the_first_character_is_taken));
        using var context = database.Context();

        int code = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.Ascii(t.Title))
            .First();

        Assert.Equal('W', (char)code);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_strings_are_joined_with_a_separator()
    {
        using var database = ForumDatabase.Filled(nameof(The_strings_are_joined_with_a_separator));
        using var context = database.Context();

        // The provider writes the rest of the strings as an array literal, and the engine does
        // not read an array literal yet:
        // "Syntax error in SQL statement "SELECT CONCAT_WS(' | ', ""f"".""Name"", ""f"".""Description"",
        // ARRAY[*][])\000d\000aFROM ""Forums"" ""f""\000d\000aORDER BY ""f"".""Id""\000d\000aLIMIT 1";
        // expected ")"" (dimzon/h2sharp#14, the array type).
        var joined = context.Forums
            .OrderBy(f => f.Id)
            .Select(f => EF.Functions.ConcatWithSeparator(" | ", f.Name, f.Description))
            .First();

        Assert.StartsWith("Programming | ", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void The_name_of_the_day_of_the_week_is_taken()
    {
        using var database = ForumDatabase.Filled(nameof(The_name_of_the_day_of_the_week_is_taken));
        using var context = database.Context();

        var day = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.DayName(t.CreatedAt))
            .First();

        Assert.Equal("Thursday", day);
    }

    [Fact]
    public void The_name_of_the_month_is_taken()
    {
        using var database = ForumDatabase.Filled(nameof(The_name_of_the_month_is_taken));
        using var context = database.Context();

        var month = context.Topics
            .OrderBy(t => t.Id)
            .Select(t => EF.Functions.MonthName(t.CreatedAt))
            .First();

        Assert.Equal("January", month);
    }

    [Fact]
    public void The_rows_are_asked_for_with_an_insensitive_like()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_an_insensitive_like));
        using var context = database.Context();

        int count = context.Topics.Count(t => EF.Functions.ILike(t.Title, "the%"));

        Assert.Equal(2, count);
    }
}
