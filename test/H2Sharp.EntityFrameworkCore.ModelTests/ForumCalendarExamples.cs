using System.Linq.Expressions;
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

public class ForumCalendarExamples
{
    [Fact]
    public void Reminder_dates_are_computed_from_birth_dates()
    {
        using var database = ForumDatabase.Filled(nameof(Reminder_dates_are_computed_from_birth_dates));
        using var context = database.Context();
        Expression<Func<User, DateOnly>> nextDay = u => u.BirthDate!.Value.AddDays(1);
        Expression<Func<User, DateOnly>> nextMonth = u => u.BirthDate!.Value.AddMonths(1);
        Expression<Func<User, DateOnly>> nextYear = u => u.BirthDate!.Value.AddYears(1);
        var source = ForumData.Users().Where(u => u.BirthDate.HasValue).ToArray();
        var users = context.Users.Where(u => u.BirthDate != null).OrderBy(u => u.Id);
        Assert.Equal(source.Select(nextDay.Compile()), users.Select(nextDay).ToList());
        Assert.Equal(source.Select(nextMonth.Compile()), users.Select(nextMonth).ToList());
        Assert.Equal(source.Select(nextYear.Compile()), users.Select(nextYear).ToList());
    }

    [Fact]
    public void Calendar_projects_date_and_time_components_for_delivery()
    {
        using var database = ForumDatabase.Filled(nameof(Calendar_projects_date_and_time_components_for_delivery));
        using var context = database.Context();
        Assert.Equal(ForumData.Topics().Select(t => DateOnly.FromDateTime(t.CreatedAt)),
            context.Topics.OrderBy(t => t.Id).Select(t => DateOnly.FromDateTime(t.CreatedAt)).ToList());
        Assert.Equal(ForumData.Topics().Select(t => TimeOnly.FromDateTime(t.CreatedAt)),
            context.Topics.OrderBy(t => t.Id).Select(t => TimeOnly.FromDateTime(t.CreatedAt)).ToList());
        Assert.Equal(new[] { "420", "380", "250", "90", "0" },
            context.Users.OrderBy(u => u.Id).Select(u => u.Reputation.ToString()).ToList());
        Assert.Equal(new[] { "False", "False", "False", "False", "True" },
            context.Users.OrderBy(u => u.Id).Select(u => u.IsBanned.ToString()).ToList());
    }

    [Fact]
    public void Delivery_slots_group_users_by_preferred_hour_and_birth_weekday()
    {
        using var database = ForumDatabase.Filled(nameof(Delivery_slots_group_users_by_preferred_hour_and_birth_weekday));
        using var context = database.Context();

        // H2 reads an expression in ORDER BY, but the engine of this port only reads a
        // column or an alias there (dimzon/h2sharp#21), so the rows are ordered in .NET.
        Assert.Equal(
            ForumData.Users().Where(u => u.PreferredTime.HasValue).Select(u => u.PreferredTime!.Value.Hour).OrderBy(h => h).ToArray(),
            context.Users.Where(u => u.PreferredTime != null).Select(u => u.PreferredTime!.Value.Hour).ToList().OrderBy(h => h).ToArray());
        Assert.Equal(
            ForumData.Users().Where(u => u.BirthDate.HasValue).Select(u => u.BirthDate!.Value.DayOfWeek).OrderBy(d => d).ToArray(),
            context.Users.Where(u => u.BirthDate != null).Select(u => u.BirthDate!.Value.DayOfWeek).ToList().OrderBy(d => d).ToArray());
    }

    [Fact]
    public void Birthdays_group_by_month_and_times_project_their_parts()
    {
        using var database = ForumDatabase.Filled(nameof(Birthdays_group_by_month_and_times_project_their_parts));
        using var context = database.Context();

        Assert.Equal(
            ForumData.Users().Where(u => u.BirthDate.HasValue).GroupBy(u => u.BirthDate!.Value.Month)
                .OrderBy(g => g.Key).Select(g => (g.Key, g.Count()))
                .ToArray(),
            context.Users.Where(u => u.BirthDate != null)
                .GroupBy(u => u.BirthDate!.Value.Month).OrderBy(g => g.Key).Select(g => new { Month = g.Key, Count = g.Count() })
                .ToList().Select(g => (g.Month, g.Count)).ToArray());
        Assert.Equal(
            [12, 1, 30],
            context.Users.Where(u => u.BirthDate != null).OrderBy(u => u.Id).Select(u => u.BirthDate!.Value.Day).ToList());
        Assert.Equal(
            [1990, 1985, 1992],
            context.Users.Where(u => u.BirthDate != null).OrderBy(u => u.Id).Select(u => u.BirthDate!.Value.Year).ToList());
        Assert.Equal(
            [30, 0, 15],
            context.Users.Where(u => u.PreferredTime != null).OrderBy(u => u.Id).Select(u => u.PreferredTime!.Value.Minute).ToList());
        Assert.Equal(
            [0, 0, 0],
            context.Users.Where(u => u.PreferredTime != null).OrderBy(u => u.Id).Select(u => u.PreferredTime!.Value.Second).ToList());
        Assert.Equal(
            ForumData.Topics().Select(t => DateOnly.FromDateTime(t.CreatedAt)),
            context.Topics.OrderBy(t => t.Id).Select(t => DateOnly.FromDateTime(t.CreatedAt)).ToList());
    }

    [Fact]
    public void Calendar_day_numbers_are_computed_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(Calendar_day_numbers_are_computed_on_reference_h2));
        using var context = database.Context();
        var numbers = context.Users.Where(u => u.BirthDate != null).OrderBy(u => u.Id).Select(u => u.BirthDate!.Value.DayNumber);
        Assert.Equal(ForumData.Users().Where(u => u.BirthDate.HasValue).Select(u => u.BirthDate!.Value.DayNumber.ToString()),
            ReferenceForum.Query(context, numbers).Select(row => row[0]));
        var dates = context.Topics.OrderBy(t => t.Id).Select(t => DateOnly.FromDayNumber(t.CreatedAt.DayOfYear + 738884));
        Assert.Equal("2024-01-11", ReferenceForum.Query(context, dates)[0][0]);
    }

    [Fact]
    public void Calendar_day_numbers_are_computed_on_the_port()
    {
        using var database = ForumDatabase.Filled(nameof(Calendar_day_numbers_are_computed_on_the_port));
        using var context = database.Context();
        Assert.Equal(ForumData.Users().Where(u => u.BirthDate.HasValue).Select(u => u.BirthDate!.Value.DayNumber),
            context.Users.Where(u => u.BirthDate != null).OrderBy(u => u.Id).Select(u => u.BirthDate!.Value.DayNumber).ToList());
    }
}
