using System.Globalization;
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for with the parts of a date and with the numbers.
/// </summary>
public class ForumDateAndMathQueryTests
{
    [Fact]
    public void View_counts_are_combined_with_bit_operations()
    {
        using var database = ForumDatabase.Filled(nameof(View_counts_are_combined_with_bit_operations));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // H2 2.5 has no "&", "|" or "^": the reference reads BITAND, BITOR and BITXOR
        // instead, and the provider writes those. A nested combination also checks that
        // the functions compose without extra brackets. Shifts ("<<" and ">>") the base
        // translation declines, so they are answered client-side, like upstream.
        var query = context.Topics.OrderBy(t => t.Id).Select(
            t => new
            {
                And = t.ViewCount & 6,
                Or = t.ViewCount | 1,
                Xor = t.ViewCount ^ 3,
                Nested = (t.ViewCount & 6) | 1
            });
        var expected = source.Select(
            t => new
            {
                And = t.ViewCount & 6,
                Or = t.ViewCount | 1,
                Xor = t.ViewCount ^ 3,
                Nested = (t.ViewCount & 6) | 1
            }).ToArray();

        Assert.Equal(expected, query.ToList());
        Assert.Equal(
            expected.Select(x => x.And).ToArray(),
            ReferenceForum.Query(context, query.Select(x => x.And)).Select(row => int.Parse(row[0]!, CultureInfo.InvariantCulture)).ToArray());
    }

    [Fact]
    public void View_counts_are_negated_twice_and_notes_are_not_null_checked()
    {
        using var database = ForumDatabase.Filled(nameof(View_counts_are_negated_twice_and_notes_are_not_null_checked));
        using var context = database.Context();

        // A double negation keeps the value: the brackets stay where the nesting is.
        Assert.Equal(
            ForumData.Topics().OrderBy(t => t.Id).Select(t => -(-t.ViewCount)),
            context.Topics.OrderBy(t => t.Id).Select(t => -(-t.ViewCount)).ToList());

        // A negated null check reads the opposite rows.
        Assert.Equal(
            ForumData.Topics().Where(t => !(t.ModeratorNote == null)).Select(t => t.Title).OrderBy(t => t),
            context.Topics.Where(t => !(t.ModeratorNote == null)).Select(t => t.Title).OrderBy(t => t).ToList());
    }

    [Fact]
    public void Topics_are_moved_by_a_day_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(Topics_are_moved_by_a_day_on_reference_h2));
        using var context = database.Context();

        // H2 2.5 adds an interval with "+": the engine of this port does not read it yet
        // (dimzon/h2sharp#20, operations on dates), so the reference checks the statement.
        var moved = context.Topics
            .Where(t => t.CreatedAt + TimeSpan.FromDays(1) > new DateTime(2024, 1, 10))
            .OrderBy(t => t.Id).Select(t => t.Title);
        Assert.Equal(
            ForumData.Topics().Where(t => t.CreatedAt + TimeSpan.FromDays(1) > new DateTime(2024, 1, 10)).Select(t => t.Title),
            ReferenceForum.Query(context, moved).Select(row => row[0]));
    }

    [Fact]
    public void View_counts_are_clamped_with_greatest_and_least()
    {
        using var database = ForumDatabase.Filled(nameof(View_counts_are_clamped_with_greatest_and_least));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // H2 2.5 reads GREATEST and LEAST the way .NET Math.Max and Math.Min answer.
        Assert.Equal(
            source.Select(t => Math.Max(t.ViewCount, 1000)),
            context.Topics.OrderBy(t => t.Id).Select(t => Math.Max(t.ViewCount, 1000)).ToList());
        Assert.Equal(
            source.Select(t => Math.Min(t.ViewCount, 1000)),
            context.Topics.OrderBy(t => t.Id).Select(t => Math.Min(t.ViewCount, 1000)).ToList());
    }

    [Fact]
    public void Dates_are_built_from_their_parts()
    {
        using var database = ForumDatabase.Filled(nameof(Dates_are_built_from_their_parts));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // H2 2.5 has no make_date or make_timestamp: the parts are concatenated and
        // cast, the way a timestamp literal reads.
        Assert.Equal(
            source.Select(t => new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day)),
            context.Topics.OrderBy(t => t.Id).Select(t => new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day)).ToList());
        Assert.Equal(
            source.Select(t => new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day, t.CreatedAt.Hour, t.CreatedAt.Minute, t.CreatedAt.Second)),
            context.Topics.OrderBy(t => t.Id).Select(t => new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day, t.CreatedAt.Hour, t.CreatedAt.Minute, t.CreatedAt.Second)).ToList());
        Assert.Equal(
            source.Select(t => new DateOnly(t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day)),
            context.Topics.OrderBy(t => t.Id).Select(t => new DateOnly(t.CreatedAt.Year, t.CreatedAt.Month, t.CreatedAt.Day)).ToList());

        // Construction inside a predicate cannot move to the client: the reference
        // reads the generated concatenation and cast as the literal would.
        var rebuilt = context.Topics
            .Where(t => t.CreatedAt >= new DateTime(t.CreatedAt.Year, 1, 1))
            .OrderBy(t => t.Id).Select(t => t.Title);
        Assert.Equal(
            ForumData.Topics().Select(t => t.Title),
            ReferenceForum.Query(context, rebuilt).Select(row => row[0]));
    }

    [Fact]
    public void The_year_of_a_date_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_year_of_a_date_is_asked_for));
        using var context = database.Context();

        Assert.Equal(2024, context.Topics.Max(t => t.CreatedAt.Year));
        Assert.Equal(2024, context.Topics.Min(t => t.CreatedAt.Year));
    }

    [Fact]
    public void The_month_of_a_date_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_month_of_a_date_is_asked_for));
        using var context = database.Context();

        Assert.Equal(3, context.Topics.Max(t => t.CreatedAt.Month));
        Assert.Equal(1, context.Topics.Min(t => t.CreatedAt.Month));
    }

    [Fact]
    public void The_day_of_a_date_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_day_of_a_date_is_asked_for));
        using var context = database.Context();

        Assert.Equal(26, context.Topics.Max(t => t.CreatedAt.Day));
        Assert.Equal(1, context.Topics.Min(t => t.CreatedAt.Day));
    }

    [Fact]
    public void The_hour_of_a_date_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_hour_of_a_date_is_asked_for));
        using var context = database.Context();

        Assert.Equal(12, context.Posts.Max(p => p.CreatedAt.Hour));
    }

    [Fact]
    public void The_first_hour_of_a_date_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_first_hour_of_a_date_is_asked_for));
        using var context = database.Context();

        // The seed puts no post at midnight: the hours of the posts are 9, 10, 11, 12, 2 and 6,
        // and the engine keeps them as they are.
        Assert.Equal(0, context.Posts.Count(p => p.CreatedAt.Hour == 0));
        Assert.Equal(7, context.Posts.Count(p => p.CreatedAt.Hour == 9));
    }

    [Fact]
    public void The_day_of_the_week_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_day_of_the_week_is_asked_for));
        using var context = database.Context();

        Assert.Equal(6, context.Topics.Max(t => (int)t.CreatedAt.DayOfWeek));
        Assert.Equal(0, context.Topics.Min(t => (int)t.CreatedAt.DayOfWeek));
    }

    [Fact]
    public void The_day_of_the_year_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_day_of_the_year_is_asked_for));
        using var context = database.Context();

        Assert.Equal(61, context.Topics.Max(t => t.CreatedAt.DayOfYear));
        Assert.Equal(11, context.Topics.Min(t => t.CreatedAt.DayOfYear));
    }

    [Fact]
    public void The_date_is_taken_from_a_date_and_the_time()
    {
        using var database = ForumDatabase.Filled(nameof(The_date_is_taken_from_a_date_and_the_time));
        using var context = database.Context();

        var date = context.Topics.OrderBy(t => t.Id).Select(t => t.CreatedAt.Date).First();

        Assert.Equal(new DateTime(2024, 1, 11), date);
    }

    [Fact]
    public void The_time_is_taken_from_a_date()
    {
        using var database = ForumDatabase.Filled(nameof(The_time_is_taken_from_a_date));
        using var context = database.Context();

        var time = context.Posts.OrderBy(p => p.Id).Select(p => p.CreatedAt.TimeOfDay).First();

        Assert.Equal(new TimeSpan(10, 0, 0), time);
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_year_of_a_date()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_year_of_a_date));
        using var context = database.Context();

        Assert.Equal(7, context.Topics.Count(t => t.CreatedAt.Year == 2024));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_month_of_a_date()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_month_of_a_date));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => t.CreatedAt.Month == 2));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_hour_of_a_date()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_hour_of_a_date));
        using var context = database.Context();

        Assert.Equal(2, context.Posts.Count(p => p.CreatedAt.Hour == 10));
    }

    [Fact]
    public void The_rows_are_asked_for_with_the_day_of_the_week()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_the_day_of_the_week));
        using var context = database.Context();

        Assert.Equal(2, context.Topics.Count(t => t.CreatedAt.DayOfWeek == DayOfWeek.Tuesday));
    }

    [Fact]
    public void The_absolute_value_of_a_number_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_absolute_value_of_a_number_is_asked_for));
        using var context = database.Context();

        int total = context.Users.Sum(u => Math.Abs(u.Reputation));

        Assert.Equal(1140, total);
    }

    [Fact]
    public void The_absolute_value_of_a_negative_number_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_absolute_value_of_a_negative_number_is_asked_for));
        using var context = database.Context();

        // The provider writes the negation as the difference from zero, "(0 - N)", because the
        // engine does not read the negation of an expression: "SELECT -N FROM T" is refused
        // with "expected number", and so is "SELECT ABS(-N) FROM T".
        int total = context.Users.Sum(u => Math.Abs(-u.Reputation));

        Assert.Equal(1140, total);
    }

    [Fact]
    public void The_rounded_number_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_rounded_number_is_asked_for));
        using var context = database.Context();

        var balances = context.Users.OrderBy(u => u.Id).Select(u => Math.Round(u.Balance, 1)).ToList();

        Assert.Equal(12.5m, balances[0]);
        Assert.Equal(0.3m, balances[2]);
    }

    [Fact]
    public void The_topics_created_after_a_cutoff_date_are_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_topics_created_after_a_cutoff_date_are_asked_for));
        using var context = database.Context();

        var cutoff = new DateTime(2024, 2, 1);
        Assert.Equal(
            ForumData.Topics().Where(t => t.CreatedAt > cutoff).Select(t => t.Title),
            context.Topics.Where(t => t.CreatedAt > cutoff).OrderBy(t => t.Id).Select(t => t.Title).ToList());
    }

    [Fact]
    public void The_hours_between_the_writes_are_asked_for_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(The_hours_between_the_writes_are_asked_for_on_reference_h2));
        using var context = database.Context();

        // Subtracting timestamps is H2 arithmetic; the engine does not read date and time
        // operations yet, so the port run below stays red until it does.
        var hours = context.Topics.OrderBy(t => t.Id).Select(t => (t.LastPostAt - t.CreatedAt).TotalHours);
        Assert.Equal(
            ForumData.Topics().Select(t => (t.LastPostAt - t.CreatedAt).TotalHours),
            ReferenceForum.Query(context, hours).Select(row => double.Parse(row[0]!, CultureInfo.InvariantCulture)).ToArray());
    }

    [Fact]
    public void The_hours_between_the_writes_are_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_hours_between_the_writes_are_asked_for));
        using var context = database.Context();

        // The provider counts whole hours with DATEDIFF, which the engine reads; the raw
        // "-" of two timestamps is still its gap (dimzon/h2sharp#20). DATEDIFF counts whole
        // unit boundaries: the same holds for minutes and seconds on these aligned rows.
        var hours = context.Topics.OrderBy(t => t.Id).Select(t => (t.LastPostAt - t.CreatedAt).TotalHours).ToList();

        Assert.Equal(
            ForumData.Topics().Select(t => (t.LastPostAt - t.CreatedAt).TotalHours),
            hours);
        Assert.Equal(
            ForumData.Topics().Select(t => (t.LastPostAt - t.CreatedAt).TotalMinutes).ToArray(),
            context.Topics.OrderBy(t => t.Id).Select(t => (t.LastPostAt - t.CreatedAt).TotalMinutes).ToArray());
        Assert.Equal(
            ForumData.Topics().Select(t => (t.LastPostAt - t.CreatedAt).TotalSeconds).ToArray(),
            context.Topics.OrderBy(t => t.Id).Select(t => (t.LastPostAt - t.CreatedAt).TotalSeconds).ToArray());
    }

    [Fact]
    public void The_number_is_asked_for_with_a_sum_and_a_count()
    {
        using var database = ForumDatabase.Filled(nameof(The_number_is_asked_for_with_a_sum_and_a_count));
        using var context = database.Context();

        int total = context.Topics.Sum(t => t.ViewCount);
        int count = context.Topics.Count();

        Assert.Equal(3680, total);
        Assert.Equal(7, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_divided_number()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_divided_number));
        using var context = database.Context();

        int count = context.Topics.Count(t => t.ViewCount / 100 == 12);

        Assert.Equal(1, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_multiplied_number()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_multiplied_number));
        using var context = database.Context();

        int count = context.Topics.Count(t => t.ViewCount * 2 == 2400);

        Assert.Equal(1, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_remainder()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_remainder));
        using var context = database.Context();

        int count = context.Topics.Count(t => t.ViewCount % 100 == 50);

        Assert.Equal(1, count);
    }
}
