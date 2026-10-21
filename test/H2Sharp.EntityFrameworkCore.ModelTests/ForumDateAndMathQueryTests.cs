using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for with the parts of a date and with the numbers.
/// </summary>
public class ForumDateAndMathQueryTests
{
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
