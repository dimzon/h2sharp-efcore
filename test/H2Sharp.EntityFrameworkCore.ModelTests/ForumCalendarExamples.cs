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
