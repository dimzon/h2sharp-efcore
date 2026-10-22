using System.Linq.Expressions;
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
    public void Null_notes_and_negative_insensitive_patterns_are_filtered()
    {
        using var database = ForumDatabase.Filled(nameof(Null_notes_and_negative_insensitive_patterns_are_filtered));
        using var context = database.Context();
        Assert.Equal(5, context.Topics.Count(t => string.IsNullOrEmpty(t.ModeratorNote)));
        Assert.Equal(5, context.Topics.Count(t => !EF.Functions.ILike(t.Title, "the%", "\\")));
        Assert.Equal(4, context.Topics.Count(t => !EF.Functions.Like(t.Title, "%never%", "\\") && t.ModeratorNote == null));
    }
}
