using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Missing notes, dates and fallbacks: nulls are asked and replaced in the query.
/// </summary>
public class ForumNullExamples
{
    [Fact]
    public void Notes_fall_back_to_a_default_text()
    {
        using var database = ForumDatabase.Filled(nameof(Notes_fall_back_to_a_default_text));
        using var context = database.Context();

        var notes = context.Topics.OrderBy(t => t.Id).Select(t => t.ModeratorNote ?? "Open").ToList();

        Assert.Equal(ForumData.Topics().Select(t => t.ModeratorNote ?? "Open"), notes);
    }

    [Fact]
    public void Rows_with_and_without_notes_are_counted()
    {
        using var database = ForumDatabase.Filled(nameof(Rows_with_and_without_notes_are_counted));
        using var context = database.Context();

        Assert.Equal(5, context.Topics.Count(t => t.ModeratorNote == null));
        Assert.Equal(2, context.Topics.Count(t => t.ModeratorNote != null));
        Assert.Equal(2, context.Users.Count(u => u.BirthDate == null));
    }

    [Fact]
    public void A_missing_row_comes_back_as_nothing()
    {
        using var database = ForumDatabase.Filled(nameof(A_missing_row_comes_back_as_nothing));
        using var context = database.Context();

        Assert.Null(context.Topics.FirstOrDefault(t => t.Id == 999));
        Assert.Null(context.Topics.SingleOrDefault(t => t.Id == 999));
        Assert.Equal("What a compiler does with the source", context.Topics.FirstOrDefault(t => t.Id == 1)!.Title);
    }

    [Fact]
    public void Flags_are_computed_with_a_condition()
    {
        using var database = ForumDatabase.Filled(nameof(Flags_are_computed_with_a_condition));
        using var context = database.Context();

        var states = context.Topics.OrderBy(t => t.Id).Select(t => t.IsClosed ? "closed" : "open").ToList();

        Assert.Equal(["open", "open", "closed", "open", "open", "open", "closed"], states);
    }

    [Fact]
    public void Users_seen_recently_are_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(Users_seen_recently_are_asked_for));
        using var context = database.Context();

        var names = context.Users
            .Where(u => u.LastSeenAt != null && u.LastSeenAt > ForumData.Now.AddDays(-10))
            .OrderBy(u => u.Id)
            .Select(u => u.UserName)
            .ToList();

        Assert.Equal(["ada", "grace", "linus"], names);
    }

    [Fact]
    public void Negated_and_nested_null_checks_read_as_their_opposites()
    {
        using var database = ForumDatabase.Filled(nameof(Negated_and_nested_null_checks_read_as_their_opposites));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => !(t.ModeratorNote == null)));
        Assert.Equal(
            ["The cost of an abstraction"],
            context.Topics
                .Where(t => t.ModeratorNote != null && t.ModeratorNote.StartsWith("Locked"))
                .Select(t => t.Title)
                .ToList());
        Assert.Equal(
            ["grace", "linus", "trouble"],
            context.Users
                .Where(u => u.BirthDate == null || u.BirthDate.Value.Year < 1990)
                .OrderBy(u => u.Id)
                .Select(u => u.UserName)
                .ToList());
    }

    [Fact]
    public void Birth_dates_unwrap_to_values_for_the_calendar()
    {
        using var database = ForumDatabase.Filled(nameof(Birth_dates_unwrap_to_values_for_the_calendar));
        using var context = database.Context();

        var years = context.Users
            .Where(u => u.BirthDate.HasValue)
            .OrderBy(u => u.Id)
            .Select(u => u.BirthDate!.Value.Year)
            .ToList();

        Assert.Equal(
            ForumData.Users().Where(u => u.BirthDate.HasValue).Select(u => u.BirthDate!.Value.Year),
            years);
    }

    [Fact]
    public void Birth_dates_are_rebuilt_from_their_parts()
    {
        using var database = ForumDatabase.Filled(nameof(Birth_dates_are_rebuilt_from_their_parts));
        using var context = database.Context();

        // new DateOnly(y, m, d) has no make_date in H2 either: same concatenation
        // and cast, inside a predicate where the client cannot step in.
        var query = context.Users
            .Where(u => u.BirthDate.HasValue && u.BirthDate!.Value >= new DateOnly(u.BirthDate!.Value.Year, 1, 1))
            .OrderBy(u => u.Id).Select(u => u.UserName);
        Assert.Equal(
            ForumData.Users().Where(u => u.BirthDate.HasValue).Select(u => u.UserName),
            query.ToList());
    }

    [Fact]
    public void Same_day_numbers_cancel_each_other()
    {
        using var database = ForumDatabase.Filled(nameof(Same_day_numbers_cancel_each_other));
        using var context = database.Context();

        // A day number is the date minus the epoch: subtracting the same number twice
        // cancels to a date minus itself, and every dated birth answers zero.
        var gaps = context.Users
            .Where(u => u.BirthDate.HasValue)
            .OrderBy(u => u.Id)
            .Select(u => u.BirthDate!.Value.DayNumber - u.BirthDate!.Value.DayNumber)
            .ToList();

        Assert.Equal(
            ForumData.Users().Where(u => u.BirthDate.HasValue).Select(u => 0),
            gaps);
    }
}
