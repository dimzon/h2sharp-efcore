using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for with the methods of a string.
/// </summary>
public class ForumStringQueryTests
{
    [Fact]
    public void The_string_is_turned_into_the_upper_case()
    {
        using var database = ForumDatabase.Filled(nameof(The_string_is_turned_into_the_upper_case));
        using var context = database.Context();

        var title = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.ToUpper()).First();

        Assert.Equal("WHAT A COMPILER DOES WITH THE SOURCE", title);
    }

    [Fact]
    public void The_string_is_turned_into_the_lower_case()
    {
        using var database = ForumDatabase.Filled(nameof(The_string_is_turned_into_the_lower_case));
        using var context = database.Context();

        var title = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.ToLower()).First();

        Assert.Equal("what a compiler does with the source", title);
    }

    [Fact]
    public void A_piece_of_the_string_is_taken()
    {
        using var database = ForumDatabase.Filled(nameof(A_piece_of_the_string_is_taken));
        using var context = database.Context();

        var piece = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.Substring(0, 3)).First();

        Assert.Equal("Wha", piece);
    }

    [Fact]
    public void The_position_of_a_piece_of_the_string_is_found()
    {
        using var database = ForumDatabase.Filled(nameof(The_position_of_a_piece_of_the_string_is_found));
        using var context = database.Context();

        int position = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.IndexOf("compiler")).First();

        Assert.Equal(7, position);
    }

    [Fact]
    public void A_piece_of_the_string_is_replaced()
    {
        using var database = ForumDatabase.Filled(nameof(A_piece_of_the_string_is_replaced));
        using var context = database.Context();

        var title = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.Replace("compiler", "assembler")).First();

        Assert.Equal("What a assembler does with the source", title);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_string_is_trimmed()
    {
        using var database = ForumDatabase.Filled(nameof(The_string_is_trimmed));
        using var context = database.Context();

        // The provider writes BTRIM with the set of the characters to trim:
        // "Syntax error in SQL statement "SELECT btrim(""t"".""Title"", E[*]' \t\n\r')"; expected ")".
        var trimmed = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.Trim()).First();

        Assert.Equal("What a compiler does with the source", trimmed);
    }

    [Fact]
    public void The_string_is_padded_to_a_length()
    {
        using var database = ForumDatabase.Filled(nameof(The_string_is_padded_to_a_length));
        using var context = database.Context();

        var padded = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.PadLeft(40, '.')).First();

        Assert.Equal(40, padded.Length);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_two_strings_are_joined()
    {
        using var database = ForumDatabase.Filled(nameof(The_two_strings_are_joined));
        using var context = database.Context();

        // The provider writes the operator of the concatenation, and the engine does not read
        // it: "Syntax error in SQL statement "SELECT ""t"".""Title"" || ' (a topic)'"; expected
        // "statement"".
        var joined = context.Topics.OrderBy(t => t.Id).Select(t => t.Title + " (a topic)").First();

        Assert.Equal("What a compiler does with the source (a topic)", joined);
    }

    [Fact]
    public void The_strings_are_joined_with_a_separator()
    {
        using var database = ForumDatabase.Filled(nameof(The_strings_are_joined_with_a_separator));
        using var context = database.Context();

        var joined = context.Forums
            .OrderBy(f => f.Id)
            .Select(f => string.Join(", ", new[] { f.Name, f.Description }))
            .First();

        Assert.Equal("Programming, Languages, compilers and the tools around them.", joined);
    }

    [Fact]
    public void The_string_is_measured()
    {
        using var database = ForumDatabase.Filled(nameof(The_string_is_measured));
        using var context = database.Context();

        int length = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.Length).First();

        Assert.Equal(36, length);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_padded_string()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_padded_string));
        using var context = database.Context();

        int count = context.Topics.Count(t => t.Title.PadLeft(40, '.').EndsWith("source"));

        Assert.Equal(1, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_an_upper_cased_string()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_an_upper_cased_string));
        using var context = database.Context();

        int count = context.Topics.Count(t => t.Title.ToUpper() == "THE COST OF AN ABSTRACTION");

        Assert.Equal(1, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_replaced_piece()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_replaced_piece));
        using var context = database.Context();

        int count = context.Topics.Count(t => t.Title.Replace("The", "A").StartsWith("A cost"));

        Assert.Equal(1, count);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_rows_are_asked_for_with_a_trimmed_string()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_trimmed_string));
        using var context = database.Context();

        // The provider writes BTRIM with the set of the characters to trim, and the engine does
        // not read that form of the function.
        int count = context.Topics.Count(t => t.Body.Trim() == "A walk through the phases, from the characters to the code.");

        Assert.Equal(1, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_concatenated_string()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_concatenated_string));
        using var context = database.Context();

        int count = context.Topics.Count(t => t.Title.Length + t.Body.Length > 40);

        Assert.Equal(7, count);
    }
}
