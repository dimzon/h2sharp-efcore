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
    public void A_single_character_is_trimmed_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(A_single_character_is_trimmed_on_reference_h2));
        using var context = database.Context();
        // The provider writes BTRIM with the set of one character; the reference answers the
        // same statement, so the SQL is checked against H2 itself as well as against the port.
        var query = context.Topics.OrderBy(t => t.Id).Select(t => t.Title.Trim('W'));
        var expected = ForumData.Topics().OrderBy(t => t.Id).Select(t => t.Title.Trim('W'));

        Assert.Equal(expected, query.ToList());
        Assert.Equal(
            expected,
            ReferenceForum.Query(context, query).Select(row => row[0]));
    }

    [Fact]
    public void A_null_or_blank_note_is_asked_about_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(A_null_or_blank_note_is_asked_about_on_reference_h2));
        using var context = database.Context();

        // The provider writes BTRIM with every character .NET calls whitespace; the reference
        // answers the same statement, so the SQL is checked against H2 itself as well.
        var query = context.Topics.OrderBy(t => t.Id).Select(t => string.IsNullOrWhiteSpace(t.ModeratorNote));
        var expected = ForumData.Topics().OrderBy(t => t.Id).Select(t => string.IsNullOrWhiteSpace(t.ModeratorNote));

        Assert.Equal(expected, query.ToList());
        Assert.Equal(
            expected,
            ReferenceForum.Query(context, query).Select(row => bool.Parse(row[0]!)));
    }

    [Fact]
    public void A_list_written_in_the_query_is_joined_into_one_text()
    {
        using var database = ForumDatabase.Filled(nameof(A_list_written_in_the_query_is_joined_into_one_text));
        using var context = database.Context();
        var first = ForumData.Topics().OrderBy(t => t.Id).First();

        // An array written in the query is not a parameter: CONCAT_WS takes its elements,
        // and a null one becomes an empty text.
        var joined = context.Topics.OrderBy(t => t.Id)
            .Select(t => string.Join(" | ", new[] { t.Title, t.ModeratorNote }))
            .First();

        Assert.Equal($"{first.Title} | ", joined);
    }

    [Fact]
    public void The_string_is_trimmed()
    {
        using var database = ForumDatabase.Filled(nameof(The_string_is_trimmed));
        using var context = database.Context();

        // The provider writes BTRIM with the set of the characters .NET's Trim treats as
        // whitespace, spelled as a U& literal because the set has non-ASCII members.
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
    public void The_two_strings_are_joined()
    {
        using var database = ForumDatabase.Filled(nameof(The_two_strings_are_joined));
        using var context = database.Context();

        // The provider joins the texts with CONCAT, the function H2 2.5 has. The operator of
        // PostgreSQL ("a || b") means something else: it makes the whole result NULL when one
        // of the parts is NULL, while CONCAT ignores the NULL parts. The reference H2 2.5 was
        // asked about both: CONCAT('a', NULL) is 'a', and 'a' || NULL is NULL.
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
    public void The_rows_are_asked_for_with_a_trimmed_string()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_trimmed_string));
        using var context = database.Context();

        // The provider writes BTRIM with the whitespace set, and the engine reads it.
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
