using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The moments of the forum are moved around: by calendar units, by the parts of a day,
///     and across the boundary between a date and a time.
/// </summary>
public class ForumClockArithmeticExamples
{
    [Fact]
    public void The_moment_is_moved_by_whole_calendar_units()
    {
        using var database = ForumDatabase.Filled(nameof(The_moment_is_moved_by_whole_calendar_units));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();
        var topics = context.Topics.OrderBy(t => t.Id);

        Assert.Equal(
            source.Select(t => t.CreatedAt.AddYears(1)),
            topics.Select(t => t.CreatedAt.AddYears(1)).ToList());
        Assert.Equal(
            source.Select(t => t.CreatedAt.AddMonths(2)),
            topics.Select(t => t.CreatedAt.AddMonths(2)).ToList());
        Assert.Equal(
            source.Select(t => t.CreatedAt.AddDays(10)),
            topics.Select(t => t.CreatedAt.AddDays(10)).ToList());
    }

    [Fact]
    public void The_moment_is_moved_by_the_parts_of_a_day()
    {
        using var database = ForumDatabase.Filled(nameof(The_moment_is_moved_by_the_parts_of_a_day));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();
        var topics = context.Topics.OrderBy(t => t.Id);

        Assert.Equal(
            source.Select(t => t.CreatedAt.AddHours(5)),
            topics.Select(t => t.CreatedAt.AddHours(5)).ToList());
        Assert.Equal(
            source.Select(t => t.CreatedAt.AddMinutes(90)),
            topics.Select(t => t.CreatedAt.AddMinutes(90)).ToList());
        Assert.Equal(
            source.Select(t => t.CreatedAt.AddSeconds(30)),
            topics.Select(t => t.CreatedAt.AddSeconds(30)).ToList());
    }

    [Fact]
    public void The_time_is_moved_by_hours_and_minutes()
    {
        using var database = ForumDatabase.Filled(nameof(The_time_is_moved_by_hours_and_minutes));
        using var context = database.Context();
        var source = ForumData.Users().Where(u => u.PreferredTime.HasValue).OrderBy(u => u.Id).ToArray();
        var users = context.Users.Where(u => u.PreferredTime != null).OrderBy(u => u.Id);

        Assert.Equal(
            source.Select(u => u.PreferredTime!.Value.AddHours(2)),
            users.Select(u => u.PreferredTime!.Value.AddHours(2)).ToList());
        Assert.Equal(
            source.Select(u => u.PreferredTime!.Value.AddMinutes(45)),
            users.Select(u => u.PreferredTime!.Value.AddMinutes(45)).ToList());
        Assert.Equal(
            source.Select(u => u.PreferredTime!.Value.Add(TimeSpan.FromMinutes(45))),
            users.Select(u => u.PreferredTime!.Value.Add(TimeSpan.FromMinutes(45))).ToList());
    }

    [Fact]
    public void The_moment_is_carried_between_zones_and_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_moment_is_carried_between_zones_and_back));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        Assert.Equal(
            source.Select(t => t.CreatedAt.ToUniversalTime().ToLocalTime()),
            context.Topics.OrderBy(t => t.Id).Select(t => t.CreatedAt.ToUniversalTime().ToLocalTime()).ToList());
    }

    [Fact]
    public void The_kind_of_a_moment_is_restated()
    {
        using var database = ForumDatabase.Filled(nameof(The_kind_of_a_moment_is_restated));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // SpecifyKind changes the declared kind and keeps the wall clock; a timestamp is
        // already the kind the engine stores, so no conversion is written.
        Assert.Equal(
            source.Select(t => t.CreatedAt),
            context.Topics.OrderBy(t => t.Id).Select(t => DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Unspecified)).ToList());
        Assert.Equal(
            source.Select(t => t.CreatedAt),
            context.Topics.OrderBy(t => t.Id).Select(t => DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Local)).ToList());
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_kind_of_a_moment_is_declared_utc()
    {
        using var database = ForumDatabase.Filled(nameof(The_kind_of_a_moment_is_declared_utc));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // The wall clock of a timestamp read as UTC is the same value restated with a zone,
        // which H2 writes with AT TIME ZONE. The port reads neither that operator nor a
        // timestamp with a time zone as a DateTime.
        Assert.Equal(
            source.Select(t => t.CreatedAt),
            context.Topics.OrderBy(t => t.Id).Select(t => DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc)).ToList());
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_moment_is_converted_to_utc()
    {
        using var database = ForumDatabase.Filled(nameof(The_moment_is_converted_to_utc));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // The timestamp is cast to a timestamp with a time zone, and H2 reads a cast that way
        // in the zone of the session -- the local time ConvertTimeToUtc starts from. The port
        // runs the cast, but reads the value as DateTimeOffset and refuses to give it out as
        // DateTime.
        Assert.Equal(
            source.Select(t => TimeZoneInfo.ConvertTimeToUtc(t.CreatedAt)),
            context.Topics.OrderBy(t => t.Id).Select(t => TimeZoneInfo.ConvertTimeToUtc(t.CreatedAt)).ToList());
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_moment_is_converted_to_a_named_zone()
    {
        using var database = ForumDatabase.Filled(nameof(The_moment_is_converted_to_a_named_zone));
        using var context = database.Context();
        var source = ForumData.Topics().OrderBy(t => t.Id).ToArray();

        // AT TIME ZONE is H2 2.5 syntax, and the port does not read it yet.
        Assert.Equal(
            source.Select(t => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(t.CreatedAt.ToUniversalTime(), "UTC")),
            context.Topics.OrderBy(t => t.Id)
                .Select(t => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(t.CreatedAt.ToUniversalTime(), "UTC")).ToList());
    }

    [Fact]
    public void The_time_is_turned_into_a_duration_and_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_time_is_turned_into_a_duration_and_back));
        using var context = database.Context();
        var source = ForumData.Users().Where(u => u.PreferredTime.HasValue).OrderBy(u => u.Id).ToArray();
        var users = context.Users.Where(u => u.PreferredTime != null).OrderBy(u => u.Id);

        var spans = users.Select(u => u.PreferredTime!.Value.ToTimeSpan());
        Assert.Equal(
            source.Select(u => u.PreferredTime!.Value.ToTimeSpan()),
            spans.ToList());
        Assert.Equal(
            source.Select(u => TimeOnly.FromTimeSpan(u.PreferredTime!.Value.ToTimeSpan())),
            users.Select(u => TimeOnly.FromTimeSpan(u.PreferredTime!.Value.ToTimeSpan())).ToList());
        Assert.Equal(source.Length, ReferenceForum.Query(context, spans).Count);
    }

    [Fact]
    public void The_time_lies_between_two_times()
    {
        using var database = ForumDatabase.Filled(nameof(The_time_lies_between_two_times));
        using var context = database.Context();

        var start = new TimeOnly(8, 0);
        var end = new TimeOnly(17, 0);

        Assert.Equal(
            ForumData.Users().Count(u => u.PreferredTime is { } time && time.IsBetween(start, end)),
            context.Users.Count(u => u.PreferredTime != null && u.PreferredTime!.Value.IsBetween(start, end)));
    }

    [Fact]
    public void The_date_and_the_time_are_joined_back_into_a_moment()
    {
        using var database = ForumDatabase.Filled(nameof(The_date_and_the_time_are_joined_back_into_a_moment));
        using var context = database.Context();
        var source = ForumData.Users()
            .Where(u => u.BirthDate.HasValue && u.PreferredTime.HasValue)
            .OrderBy(u => u.Id)
            .ToArray();
        var users = context.Users
            .Where(u => u.BirthDate != null && u.PreferredTime != null)
            .OrderBy(u => u.Id);

        Assert.Equal(
            source.Select(u => u.BirthDate!.Value.ToDateTime(u.PreferredTime!.Value)),
            users.Select(u => u.BirthDate!.Value.ToDateTime(u.PreferredTime!.Value)).ToList());
    }
}
