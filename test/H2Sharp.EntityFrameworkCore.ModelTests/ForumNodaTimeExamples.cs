using H2Sharp.Data;
using NodaTime;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     NodaTime values in a forum schedule: the parts of a date, a time and a date with time are
///     asked of the database, the way a calendar or a digest does.
/// </summary>
/// <remarks>
///     The plugin maps <see cref="LocalDate" /> to <c>date</c>, <see cref="LocalTime" /> to
///     <c>time</c> and <see cref="LocalDateTime" /> to <c>timestamp</c>, and the member translator
///     turns their part members into the H2 part functions. The types whose column carries a time
///     zone are not translated -- see the plugin's remarks -- and one test below holds EF to that
///     refusal.
/// </remarks>
public class ForumNodaTimeExamples
{
    private static readonly LocalDateTime MarchMeeting = new(2024, 3, 15, 9, 30);
    private static readonly LocalDateTime AprilMeeting = new(2024, 4, 20, 18, 45);

    [Fact]
    public void Parts_of_local_dates_answer_as_written()
    {
        using var connection = Open();
        using var context = new ScheduleContext(connection);

        var parts = context.Meetings.Where(
                m => m.Day.Year == 2024
                    && m.Day.Month == 3
                    && m.Day.Day == 15
                    && m.Day.DayOfYear == 75
                    && m.Day.DayOfWeek == IsoDayOfWeek.Friday)
            .Select(
                m => new { m.Day.Year, m.Day.Month, m.Day.Day, m.Day.DayOfYear, m.Day.DayOfWeek })
            .Single();

        Assert.Equal((2024, 3, 15, 75, IsoDayOfWeek.Friday), (parts.Year, parts.Month, parts.Day, parts.DayOfYear, parts.DayOfWeek));
    }

    [Fact]
    public void Parts_of_local_times_answer_as_written()
    {
        using var connection = Open();
        using var context = new ScheduleContext(connection);

        Assert.Equal(
            [(9, 30, 0), (18, 45, 0)],
            context.Meetings.OrderBy(m => m.Id).Select(
                m => new { m.StartsAt.Hour, m.StartsAt.Minute, m.StartsAt.Second })
                .ToList()
                .Select(m => (m.Hour, m.Minute, m.Second)));
    }

    [Fact]
    public void Parts_of_local_date_times_answer_as_written()
    {
        using var connection = Open();
        using var context = new ScheduleContext(connection);

        var evening = context.Meetings.Single(m => m.Opens.Hour == 18 && m.Opens.Minute == 45 && m.Opens.Second == 0);

        Assert.Equal(2, evening.Id);
        Assert.Equal(
            [(2024, 75), (2024, 111)],
            context.Meetings.OrderBy(m => m.Id)
                .Select(m => new { m.Opens.Year, m.Opens.DayOfYear })
                .ToList()
                .Select(m => (m.Year, m.DayOfYear)));
    }

    [Fact]
    public void A_zoned_date_time_member_is_refused_rather_than_answered_in_another_zone()
    {
        using var connection = Open();
        using var context = new ScheduleContext(connection);

        // OffsetDateTime and ZonedDateTime are stored as timestamp with time zone, and the part
        // functions of H2 read such a value in the zone of the session, while NodaTime asks for
        // the fields of the value's own offset. The translator refuses, and so does EF Core.
        Assert.Throws<InvalidOperationException>(
            () => context.Meetings.Where(m => m.Moment.Year == 2024).ToList());
    }

    private static H2Connection Open()
    {
        var connection = new H2Connection("jdbc:h2:mem:noda_" + Guid.NewGuid().ToString("N"));
        connection.Open();

        using (var context = new ScheduleContext(connection))
        {
            context.Database.EnsureCreated();
            context.Meetings.AddRange(
                new Meeting { Id = 1, Day = new LocalDate(2024, 3, 15), StartsAt = new LocalTime(9, 30), Opens = MarchMeeting, Moment = new OffsetDateTime(MarchMeeting, Offset.FromHours(3)) },
                new Meeting { Id = 2, Day = new LocalDate(2024, 4, 20), StartsAt = new LocalTime(18, 45), Opens = AprilMeeting, Moment = new OffsetDateTime(AprilMeeting, Offset.FromHours(-5)) });
            context.SaveChanges();
        }

        return connection;
    }

    private sealed class Meeting
    {
        public int Id { get; set; }

        public LocalDate Day { get; set; }

        public LocalTime StartsAt { get; set; }

        public LocalDateTime Opens { get; set; }

        public OffsetDateTime Moment { get; set; }
    }

    private sealed class ScheduleContext(H2Connection connection) : DbContext
    {
        public DbSet<Meeting> Meetings => Set<Meeting>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2(connection, options => options.UseNodaTime());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Meeting>().Property(m => m.Id).ValueGeneratedNever();
    }
}
