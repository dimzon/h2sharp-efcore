using System.Linq.Expressions;
using System.Text.RegularExpressions;
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Search screens use literal user input, nullable notes and per-row search terms.
/// </summary>
public class ForumSearchExamples
{
    [Theory]
    [InlineData("The")]
    [InlineData("source")]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    [InlineData("")]
    [InlineData(null)]
    public void User_search_text_is_literal_in_prefix_suffix_and_substring_search(string? text)
    {
        using var database = ForumDatabase.Filled(nameof(User_search_text_is_literal_in_prefix_suffix_and_substring_search));
        using var context = database.Context();
        var expected = ForumData.Topics();

        Assert.Equal(expected.Where(t => text != null && t.Title.StartsWith(text, StringComparison.Ordinal)).Select(t => t.Title).Order(),
            context.Topics.Where(t => t.Title.StartsWith(text!)).Select(t => t.Title).OrderBy(t => t).ToList().Order());
        Assert.Equal(expected.Where(t => text != null && t.Title.EndsWith(text, StringComparison.Ordinal)).Select(t => t.Title).Order(),
            context.Topics.Where(t => t.Title.EndsWith(text!)).Select(t => t.Title).OrderBy(t => t).ToList().Order());
        Assert.Equal(expected.Where(t => text != null && t.Title.Contains(text, StringComparison.Ordinal)).Select(t => t.Title).Order(),
            context.Topics.Where(t => t.Title.Contains(text!)).Select(t => t.Title).OrderBy(t => t).ToList().Order());
    }

    [Fact]
    public void Search_term_can_come_from_another_column()
    {
        using var database = ForumDatabase.Filled(nameof(Search_term_can_come_from_another_column));
        using var context = database.Context();
        context.Topics.Add(new Topic { Title = "operator % _ \\", Body = "%", CreatedAt = ForumData.Start, LastPostAt = ForumData.Start });
        context.SaveChanges();
        var source = context.Topics.AsNoTracking().ToList();
        Assert.Equal(source.Where(t => t.Title.Contains(t.Body, StringComparison.Ordinal)).Select(t => t.Id).Order(),
            context.Topics.Where(t => t.Title.Contains(t.Body)).Select(t => t.Id).ToList().Order());
        Assert.Equal(source.Where(t => t.Title.StartsWith(t.Body, StringComparison.Ordinal)).Select(t => t.Id).Order(),
            context.Topics.Where(t => t.Title.StartsWith(t.Body)).Select(t => t.Id).ToList().Order());
        Assert.Equal(source.Where(t => t.Title.EndsWith(t.Body, StringComparison.Ordinal)).Select(t => t.Id).Order(),
            context.Topics.Where(t => t.Title.EndsWith(t.Body)).Select(t => t.Id).ToList().Order());
    }

    [Fact]
    public void Search_results_show_note_fallback_and_title_previews()
    {
        using var database = ForumDatabase.Filled(nameof(Search_results_show_note_fallback_and_title_previews));
        using var context = database.Context();
        Expression<Func<Topic, string>> preview = t => t.Title.Substring(t.ForumId, 5) + ": " + (t.ModeratorNote ?? "Open");
        var sql = context.Topics.Select(preview).ToQueryString();
        Assert.Contains("substring", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ForumData.Topics().Select(preview.Compile()), context.Topics.OrderBy(t => t.Id).Select(preview).ToList());

        var emails = context.Users.OrderBy(u => u.Id).Select(u => u.Email.Substring(u.Email.IndexOf('@') + 1)).ToList();
        Assert.All(emails, value => Assert.Equal("example.org", value));
        var initials = context.Users.OrderBy(u => u.Id).Select(u => u.UserName.FirstOrDefault()).ToList();
        Assert.Equal(new[] { 'a', 'g', 'l', 'm', 't' }, initials);
    }

    [Fact]
    public void Case_insensitive_search_ignores_the_case_of_the_letters()
    {
        using var database = ForumDatabase.Filled(nameof(Case_insensitive_search_ignores_the_case_of_the_letters));
        using var context = database.Context();

        var titles = context.Topics
            .Where(t => t.Title.Contains("THE", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "What a compiler does with the source",
                "The cost of an abstraction",
                "Transactions and the order of the writes",
                "The forum moves",
            ],
            titles);
    }

    [Fact]
    public void Like_with_a_user_pattern()
    {
        using var database = ForumDatabase.Filled(nameof(Like_with_a_user_pattern));
        using var context = database.Context();

        var pattern = "%cost%";
        Assert.Equal(
            ["The cost of an abstraction"],
            context.Topics.Where(t => EF.Functions.Like(t.Title, pattern)).Select(t => t.Title).ToList());
    }

    [Fact]
    public void Composite_key_lookup_runs_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(Composite_key_lookup_runs_on_reference_h2));
        using var context = database.Context();

        var next = context.Topics
            .Where(t => EF.Functions.GreaterThan(
                new ValueTuple<DateTime, int>(t.CreatedAt, t.Id),
                new ValueTuple<DateTime, int>(ForumData.Start.AddDays(10), 1)))
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .Take(1)
            .Select(t => t.Title);
        Assert.Equal(["Reading a query plan"], ReferenceForum.Query(context, next).Select(row => row[0]));

        var exact = context.Topics
            .Where(t => EF.Functions.Equal(
                new ValueTuple<DateTime, int>(t.CreatedAt, t.Id),
                new ValueTuple<DateTime, int>(ForumData.Start.AddDays(10), 1)))
            .Select(t => t.Title);
        Assert.Equal(["What a compiler does with the source"], ReferenceForum.Query(context, exact).Select(row => row[0]));
    }

    [Fact]
    public void Composite_key_lookup_is_blocked_on_the_port()
    {
        using var database = ForumDatabase.Filled(nameof(Composite_key_lookup_is_blocked_on_the_port));
        using var context = database.Context();

        // A row-value comparison reads on the port now (issue dimzon/h2sharp#18 closed).
        var exact = context.Topics
            .Where(t => EF.Functions.Equal(
                new ValueTuple<DateTime, int>(t.CreatedAt, t.Id),
                new ValueTuple<DateTime, int>(ForumData.Start.AddDays(10), 1)))
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(["What a compiler does with the source"], exact);
    }

    [Fact]
    public void Multi_pattern_search_runs_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(Multi_pattern_search_runs_on_reference_h2));
        using var context = database.Context();

        // H2 has no quantified LIKE: a constant pattern list asks the same question with
        // OR (for Any) or AND (for All). The engine of this port does not read the ANY form
        // yet, so the reference checks the whole statement.
        var any = context.Topics
            .Where(t => new[] { "The%", "%plan" }.Any(p => EF.Functions.Like(t.Title, p)))
            .Select(t => t.Title);
        Assert.Equal(
            ["Reading a query plan", "The cost of an abstraction", "The forum moves"],
            ReferenceForum.Query(context, any.OrderBy(t => t)).Select(row => row[0]));

        var insensitive = context.Topics
            .Where(t => new[] { "the%", "%PLAN" }.Any(p => EF.Functions.ILike(t.Title, p)))
            .Select(t => t.Title);
        Assert.Equal(
            ["Reading a query plan", "The cost of an abstraction", "The forum moves"],
            ReferenceForum.Query(context, insensitive.OrderBy(t => t)).Select(row => row[0]));

        var all = context.Topics
            .Where(t => new[] { "%the%", "%order%" }.All(p => EF.Functions.Like(t.Title, p)))
            .Select(t => t.Title);
        Assert.Equal(
            ["Transactions and the order of the writes"],
            ReferenceForum.Query(context, all).Select(row => row[0]));
    }

    [Fact]
    public void Titles_matching_a_pattern_are_found_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(Titles_matching_a_pattern_are_found_on_reference_h2));
        using var context = database.Context();

        // H2 2.5 reads POSIX matching with ~ and ~*: the reference checks the whole statement.
        var sensitive = context.Topics.Where(t => Regex.IsMatch(t.Title, "^The")).Select(t => t.Title);
        Assert.Equal(
            ["The cost of an abstraction", "The forum moves"],
            ReferenceForum.Query(context, sensitive.OrderBy(t => t)).Select(row => row[0]));

        var insensitive = context.Topics.Where(t => Regex.IsMatch(t.Title, "^the", RegexOptions.IgnoreCase)).Select(t => t.Title);
        Assert.Equal(
            ["The cost of an abstraction", "The forum moves"],
            ReferenceForum.Query(context, insensitive.OrderBy(t => t)).Select(row => row[0]));

        // A negated match reads "!~".
        var negated = context.Topics.Where(t => !Regex.IsMatch(t.Title, "^The")).Select(t => t.Title);
        Assert.Equal(
            ["Indexes that are never used", "Reading a query plan", "Transactions and the order of the writes", "Types without a runtime", "What a compiler does with the source"],
            ReferenceForum.Query(context, negated.OrderBy(t => t)).Select(row => row[0]));

        // The inline flags are read by the reference the way .NET spells them: (?m) is
        // multiline, (?s) makes "." match newlines, (?x) ignores whitespace in the pattern.
        // The seed has no newlines, so multiline and singleline answer like the default.
        var multiline = context.Topics.Where(t => Regex.IsMatch(t.Title, "^The", RegexOptions.Multiline)).Select(t => t.Title);
        Assert.Equal(
            ["The cost of an abstraction", "The forum moves"],
            ReferenceForum.Query(context, multiline.OrderBy(t => t)).Select(row => row[0]));

        // Without newlines in the data singleline answers like the default: the check is
        // that the provider writes (?s) and the reference reads it without skewing answers.
        var singleline = context.Topics.Where(t => Regex.IsMatch(t.Title, "^The", RegexOptions.Singleline)).Select(t => t.Title);
        Assert.Equal(
            ["The cost of an abstraction", "The forum moves"],
            ReferenceForum.Query(context, singleline.OrderBy(t => t)).Select(row => row[0]));

        var whitespace = context.Topics.Where(t => Regex.IsMatch(t.Title, "^The H2", RegexOptions.IgnorePatternWhitespace)).Select(t => t.Title);
        Assert.Equal(
            [],
            ReferenceForum.Query(context, whitespace.OrderBy(t => t)).Select(row => row[0]));

        // A pattern from a variable is not a constant: the pattern is read as written.
        var pattern = "^The";
        var variable = context.Topics.Where(t => Regex.IsMatch(t.Title, pattern)).Select(t => t.Title);
        Assert.Equal(
            ["The cost of an abstraction", "The forum moves"],
            ReferenceForum.Query(context, variable.OrderBy(t => t)).Select(row => row[0]));
    }

    [Fact]
    public void Names_that_sound_like_are_found()
    {
        using var database = ForumDatabase.Filled(nameof(Names_that_sound_like_are_found));
        using var context = database.Context();

        var names = context.Users
            .Where(u => EF.Functions.Soundex(u.UserName) == EF.Functions.Soundex("ada"))
            .OrderBy(u => u.Id)
            .Select(u => u.UserName)
            .ToList();

        Assert.Contains("ada", names);
        Assert.Equal(
            4,
            context.Users.Where(u => u.UserName == "ada").Select(u => EF.Functions.SoundexDifference(u.UserName, "ada")).Single());
    }

    [Fact]
    public void Snippets_are_found_from_a_position_and_titles_are_prefixed()
    {
        using var database = ForumDatabase.Filled(nameof(Snippets_are_found_from_a_position_and_titles_are_prefixed));
        using var context = database.Context();
        var source = ForumData.Topics();

        // LOCATE with a start looks past the beginning: the same search written with
        // the 0-based offset of .NET.
        Assert.Equal(
            source.Where(t => t.Body.IndexOf("the", 9, StringComparison.Ordinal) >= 0).Select(t => t.Title).OrderBy(t => t),
            context.Topics.Where(t => EF.Functions.Locate(t.Body, "the", 10) > 0).Select(t => t.Title).OrderBy(t => t).ToList());

        // INSERT replaces LENGTH characters from the 1-based START with the new text:
        // masking the first word of a title.
        Assert.Equal(
            source.OrderBy(t => t.Id).Select(t => "****" + t.Title.Substring(4)),
            context.Topics.OrderBy(t => t.Id).Select(t => EF.Functions.Insert(t.Title, 1, 4, "****")).ToList());

        // An explicit ordinal comparison is the plain substring check.
        Assert.Equal(
            source.Where(t => t.Title.Contains("the", StringComparison.Ordinal)).Select(t => t.Title).OrderBy(t => t),
            context.Topics.Where(t => t.Title.Contains("the", StringComparison.Ordinal)).Select(t => t.Title).OrderBy(t => t).ToList());
    }

    [Fact]
    public void User_search_text_is_literal_case_insensitively()
    {
        using var database = ForumDatabase.Filled(nameof(User_search_text_is_literal_case_insensitively));
        using var context = database.Context();

        // A search box hands a variable, not a constant: the pattern is escaped at run time.
        var text = "THE";
        Assert.Equal(
            [
                "What a compiler does with the source",
                "The cost of an abstraction",
                "Transactions and the order of the writes",
                "The forum moves",
            ],
            context.Topics
                .Where(t => t.Title.Contains(text, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Id)
                .Select(t => t.Title)
                .ToList());

        // A backslash in the pattern is data, not an escape: nothing matches a literal %.
        var escaped = "100\\%";
        Assert.Empty(
            context.Topics.Where(t => EF.Functions.ILike(t.Title, escaped, "\\")).Select(t => t.Title).ToList());
    }

    [Fact]
    public void Null_notes_and_negative_insensitive_patterns_are_filtered()
    {
        using var database = ForumDatabase.Filled(nameof(Null_notes_and_negative_insensitive_patterns_are_filtered));
        using var context = database.Context();
        Assert.Equal(5, context.Topics.Count(t => string.IsNullOrEmpty(t.ModeratorNote)));
        Assert.Equal(5, context.Topics.Count(t => !EF.Functions.ILike(t.Title, "the%", "\\")));
        Assert.Equal(4, context.Topics.Count(t => !EF.Functions.Like(t.Title, "%never%", "\\") && t.ModeratorNote == null));
    }
}
