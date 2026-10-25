using Microsoft.EntityFrameworkCore;

using H2Sharp.Data;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rare types of H2 the forum model does not carry: the interval, the timestamp with a
///     time zone, the single-precision number, the small integer and the character.
/// </summary>
public class ForumMeasurementExamples
{
    [Fact]
    public void The_interval_comes_back_as_it_went_in()
    {
        using var connection = Open(nameof(The_interval_comes_back_as_it_went_in));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        var row = new Measurement { Duration = TimeSpan.FromDays(1).Add(TimeSpan.FromHours(2)), Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)), Rating = 1.5f, Small = 42, Initial = 'x' };
        context.Measurements.Add(row);
        context.SaveChanges();

        var back = context.Measurements.Single(m => m.Id == row.Id);

        Assert.Equal(row.Duration, back.Duration);
        Assert.Equal(row.Stamp, back.Stamp);
        Assert.Equal(row.Rating, back.Rating);
        Assert.Equal(row.Small, back.Small);
        Assert.Equal(row.Initial, back.Initial);
    }

    [Fact]
    public void The_stamp_keeps_a_negative_offset_and_its_fraction()
    {
        using var connection = Open(nameof(The_stamp_keeps_a_negative_offset_and_its_fraction));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        var row = new Measurement
        {
            Duration = TimeSpan.Zero,
            Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, 123, TimeSpan.FromHours(-4)),
            Rating = 1f,
            Small = 1,
            Initial = 'a'
        };
        context.Measurements.Add(row);
        context.SaveChanges();

        Assert.Equal(row.Stamp, context.Measurements.Single(m => m.Id == row.Id).Stamp);
    }

    [Fact]
    public void A_stamp_is_found_by_its_instant()
    {
        using var connection = Open(nameof(A_stamp_is_found_by_its_instant));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.Add(new Measurement
        {
            Duration = TimeSpan.Zero,
            Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)),
            Rating = 1f,
            Small = 1,
            Initial = 'a'
        });
        context.SaveChanges();

        // The same instant written with the zero offset: H2 keeps the instant, so the row is
        // found although the wall clock and the offset differ.
        var instant = new DateTimeOffset(2024, 5, 6, 4, 8, 9, TimeSpan.Zero);

        Assert.Equal(1, context.Measurements.Count(m => m.Stamp == instant));
    }

    [Fact]
    public void The_interval_keeps_a_negative_length()
    {
        using var connection = Open(nameof(The_interval_keeps_a_negative_length));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        var negative = TimeSpan.FromDays(-1).Add(TimeSpan.FromMinutes(-30));
        context.Measurements.AddRange(
            new Measurement { Duration = negative, Stamp = default, Rating = 1f, Small = 1, Initial = 'a' },
            new Measurement { Duration = TimeSpan.Zero, Stamp = default, Rating = 1f, Small = 2, Initial = 'b' });
        context.SaveChanges();

        Assert.Equal(negative, context.Measurements.Single(m => m.Small == 1).Duration);
        Assert.Equal(1, context.Measurements.Count(m => m.Duration == TimeSpan.Zero));
        Assert.Equal(1, context.Measurements.Count(m => m.Duration < TimeSpan.Zero));
    }

    [Fact]
    public void The_rare_types_find_rows()
    {
        using var connection = Open(nameof(The_rare_types_find_rows));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.AddRange(
            new Measurement { Duration = TimeSpan.FromDays(1), Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)), Rating = 1.5f, Small = 42, Initial = 'x' },
            new Measurement { Duration = TimeSpan.FromHours(1), Stamp = new DateTimeOffset(2023, 1, 2, 3, 4, 5, TimeSpan.FromHours(5)), Rating = 2.75f, Small = 7, Initial = 'y' });

        context.SaveChanges();

        Assert.Equal(1, context.Measurements.Count(m => m.Duration == TimeSpan.FromDays(1)));
        Assert.Equal(1, context.Measurements.Count(m => m.Stamp == new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3))));
        Assert.Equal(1, context.Measurements.Count(m => m.Rating == 1.5f));
        Assert.Equal(1, context.Measurements.Count(m => m.Small == 42));
        Assert.Equal(1, context.Measurements.Count(m => m.Initial == 'x'));
    }

    [Fact]
    public void The_clock_of_the_engine_is_the_instant_with_a_zone()
    {
        using var connection = Open(nameof(The_clock_of_the_engine_is_the_instant_with_a_zone));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.AddRange(
            new Measurement { Duration = TimeSpan.Zero, Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)), Rating = 1f, Small = 1, Initial = 'x' },
            new Measurement { Duration = TimeSpan.Zero, Stamp = new DateTimeOffset(2023, 1, 2, 3, 4, 5, TimeSpan.FromHours(5)), Rating = 2f, Small = 2, Initial = 'y' });
        context.SaveChanges();

        // DateTime.UtcNow is CURRENT_TIMESTAMP, a timestamp with a zone: the stamps of
        // 2024 lie before it, and the comparison keeps both sides in that type.
        Assert.Equal(2, context.Measurements.Count(m => m.Stamp < DateTime.UtcNow));
    }

    [Fact]
    public void The_stamp_is_moved_by_the_units_of_the_calendar()
    {
        using var connection = Open(nameof(The_stamp_is_moved_by_the_units_of_the_calendar));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.Add(new Measurement
        {
            Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)),
            Duration = TimeSpan.Zero,
            Rating = 1f,
            Small = 1,
            Initial = 'a'
        });
        context.SaveChanges();

        var stamp = context.Measurements.OrderBy(m => m.Id).Select(m => m.Stamp).Single();
        var moved = context.Measurements.OrderBy(m => m.Id).Select(m => new
        {
            Years = m.Stamp.AddYears(1),
            Months = m.Stamp.AddMonths(2),
            Days = m.Stamp.AddDays(3),
            Hours = m.Stamp.AddHours(4),
            Minutes = m.Stamp.AddMinutes(5),
            Seconds = m.Stamp.AddSeconds(6)
        }).Single();

        Assert.Equal(stamp.AddYears(1), moved.Years);
        Assert.Equal(stamp.AddMonths(2), moved.Months);
        Assert.Equal(stamp.AddDays(3), moved.Days);
        Assert.Equal(stamp.AddHours(4), moved.Hours);
        Assert.Equal(stamp.AddMinutes(5), moved.Minutes);
        Assert.Equal(stamp.AddSeconds(6), moved.Seconds);
    }

    [Fact]
    public void The_stamp_gives_its_parts()
    {
        using var connection = Open(nameof(The_stamp_gives_its_parts));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.Add(new Measurement
        {
            Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)),
            Duration = TimeSpan.Zero,
            Rating = 1f,
            Small = 1,
            Initial = 'a'
        });
        context.SaveChanges();

        var stamp = context.Measurements.OrderBy(m => m.Id).Select(m => m.Stamp).Single();
        var parts = context.Measurements.OrderBy(m => m.Id).Select(m => new
        {
            m.Stamp.Year,
            m.Stamp.Month,
            m.Stamp.Day,
            m.Stamp.Hour,
            m.Stamp.Minute,
            m.Stamp.Second,
            m.Stamp.DayOfWeek,
            m.Stamp.DayOfYear
        }).Single();

        // H2 reads the parts of a timestamp with a zone in the value's own offset, which is
        // what .NET asks for: the stamp was written at +03 and keeps those fields.
        Assert.Equal(stamp.Year, parts.Year);
        Assert.Equal(stamp.Month, parts.Month);
        Assert.Equal(stamp.Day, parts.Day);
        Assert.Equal(stamp.Hour, parts.Hour);
        Assert.Equal(stamp.Minute, parts.Minute);
        Assert.Equal(stamp.Second, parts.Second);
        Assert.Equal(stamp.DayOfWeek, parts.DayOfWeek);
        Assert.Equal(stamp.DayOfYear, parts.DayOfYear);
    }

    [Fact]
    public void The_stamp_gives_its_local_time()
    {
        using var connection = Open(nameof(The_stamp_gives_its_local_time));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.Add(new Measurement
        {
            Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)),
            Duration = TimeSpan.Zero,
            Rating = 1f,
            Small = 1,
            Initial = 'a'
        });
        context.SaveChanges();

        var stamp = context.Measurements.OrderBy(m => m.Id).Select(m => m.Stamp).Single();
        var local = context.Measurements.OrderBy(m => m.Id).Select(m => m.Stamp.LocalDateTime).Single();

        // A cast of a timestamp with a zone to a timestamp without one reads the value in the
        // zone of the session, which is the local time of .NET.
        Assert.Equal(stamp.LocalDateTime, local);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_stamp_gives_its_date_and_its_utc_time()
    {
        using var connection = Open(nameof(The_stamp_gives_its_date_and_its_utc_time));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.Add(new Measurement
        {
            Stamp = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(3)),
            Duration = TimeSpan.Zero,
            Rating = 1f,
            Small = 1,
            Initial = 'a'
        });
        context.SaveChanges();

        // The wall clock of the value's own offset, and the wall clock of UTC, are written out
        // by FORMATDATETIME -- a cast of the value to a timestamp would read it in the zone of
        // the session. The port does not read FORMATDATETIME yet.
        var stamp = context.Measurements.OrderBy(m => m.Id).Select(m => m.Stamp).Single();
        var parts = context.Measurements.OrderBy(m => m.Id).Select(m => new
        {
            m.Stamp.Date,
            m.Stamp.DateTime,
            m.Stamp.UtcDateTime
        }).Single();

        Assert.Equal(stamp.Date, parts.Date);
        Assert.Equal(stamp.DateTime, parts.DateTime);
        Assert.Equal(stamp.UtcDateTime, parts.UtcDateTime);
    }

    private sealed class Measurement
    {
        public int Id { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTimeOffset Stamp { get; set; }

        public float Rating { get; set; }

        public short Small { get; set; }

        public char Initial { get; set; }
    }

    [Fact]
    public void The_special_real_values_are_written_as_literals()
    {
        using var connection = Open(nameof(The_special_real_values_are_written_as_literals));
        using var context = new MeasurementContext(Options(connection));
        context.Database.EnsureCreated();

        context.Measurements.Add(new Measurement { Rating = 1.5f, Duration = TimeSpan.Zero, Stamp = default, Small = 1, Initial = 'a' });
        context.SaveChanges();

        var nan = context.Measurements.Count(m => m.Rating == float.NaN);
        var positive = context.Measurements.Count(m => m.Rating == float.PositiveInfinity);
        var negative = context.Measurements.Count(m => m.Rating == float.NegativeInfinity);
        var plain = context.Measurements.Count(m => m.Rating == 1.5f);

        Assert.Equal(0, nan);
        Assert.Equal(0, positive);
        Assert.Equal(0, negative);
        Assert.Equal(1, plain);
    }

    [Fact]
    public void An_array_column_is_asked_about_membership_on_the_port()
    {
        using var connection = Open(nameof(An_array_column_is_asked_about_membership_on_the_port));
        using var context = new ArrayContext(ArrayOptions(connection));
        context.Database.EnsureCreated();

        context.Shelves.AddRange(
            new Shelf { Id = 1, Forums = [1, 2, 3] },
            new Shelf { Id = 2, Forums = [2] },
            new Shelf { Id = 3, Forums = [3] });

        context.SaveChanges();

        var rows = context.Shelves.Where(s => s.Forums.Contains(2)).OrderBy(s => s.Id).Select(s => s.Id).ToList();

        Assert.Equal([1, 2], rows);
    }

    [Fact]
    public void The_array_of_a_row_is_read_and_asked_about()
    {
        using var connection = Open(nameof(The_array_of_a_row_is_read_and_asked_about));
        using var context = new ArrayContext(ArrayOptions(connection));
        context.Database.EnsureCreated();

        context.Shelves.AddRange(
            new Shelf { Id = 1, Forums = [1, 2, 3], Levels = [1, null, 2] },
            new Shelf { Id = 2, Forums = [2], Levels = [] },
            new Shelf { Id = 3, Forums = [3], Levels = [3] });
        context.SaveChanges();

        Assert.Equal(
            [[1, 2, 3], [2], [3]],
            context.Shelves.OrderBy(s => s.Id).Select(s => s.Forums).ToList());
        Assert.Equal(
            [[1, null, 2], [], [3]],
            context.Shelves.OrderBy(s => s.Id).Select(s => s.Levels).ToList());
        Assert.Equal([1], context.Shelves.Where(s => s.Forums.Length == 3).Select(s => s.Id).ToList());
        Assert.Equal([2], context.Shelves.Where(s => s.Forums[0] == 2).Select(s => s.Id).ToList());
        Assert.Equal([2], context.Shelves.Where(s => s.Forums.First() == 2).Select(s => s.Id).ToList());

        var subscribed = new[] { 2 };
        Assert.Equal([2], context.Shelves.Where(s => s.Forums.SequenceEqual(subscribed)).Select(s => s.Id).ToList());
    }

    [Fact]
    public void An_array_of_a_row_is_joined_with_a_list()
    {
        using var connection = Open(nameof(An_array_of_a_row_is_joined_with_a_list));
        using var context = new ArrayContext(ArrayOptions(connection));
        context.Database.EnsureCreated();

        context.Shelves.AddRange(
            new Shelf { Id = 1, Forums = [1, 2, 3] },
            new Shelf { Id = 2, Forums = [3] });
        context.SaveChanges();

        // H2 joins arrays with ARRAY_CAT. The list joined comes as a parameter, the way the
        // reference reads it.
        var extra = new[] { 9 };
        Assert.Equal(
            [1, 2],
            context.Shelves.Where(s => s.Forums.Concat(extra).Contains(9)).OrderBy(s => s.Id).Select(s => s.Id).ToList());
    }

    [Fact]
    public void The_columns_of_a_row_are_gathered_into_one_text()
    {
        using var connection = Open(nameof(The_columns_of_a_row_are_gathered_into_one_text));
        using var context = new ArrayContext(ArrayOptions(connection));
        context.Database.EnsureCreated();

        context.Shelves.AddRange(
            new Shelf { Id = 1, Forums = [1, 2, 3] },
            new Shelf { Id = 2, Forums = [2] });
        context.SaveChanges();

        Assert.Equal(
            ["1:3", "2:1"],
            context.Shelves.OrderBy(s => s.Id)
                .Select(s => string.Join(":", new[] { s.Id.ToString(), s.Forums.Length.ToString() }))
                .ToList());
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void An_array_is_built_from_the_columns_of_a_row()
    {
        using var connection = Open(nameof(An_array_is_built_from_the_columns_of_a_row));
        using var context = new ArrayContext(ArrayOptions(connection));
        context.Database.EnsureCreated();

        context.Shelves.AddRange(
            new Shelf { Id = 1, Forums = [1, 2, 3] },
            new Shelf { Id = 2, Forums = [2] });
        context.SaveChanges();

        // The reference reads ARRAY["s"."Id", "s"."Id" * 10]; the engine of this port expects
        // the closing bracket after an element and refuses an expression between the brackets.
        Assert.Equal(
            [[1, 10], [2, 20]],
            context.Shelves.OrderBy(s => s.Id).Select(s => new[] { s.Id, s.Id * 10 }).ToList());
    }

    private sealed class Shelf
    {
        public int Id { get; set; }

        public int[] Forums { get; set; } = [];

        public int?[]? Levels { get; set; } = [];
    }

    private sealed class ArrayContext : DbContext
    {
        public ArrayContext(DbContextOptions<ArrayContext> options) : base(options)
        {
        }

        public DbSet<Shelf> Shelves => Set<Shelf>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Shelf>().ToTable("Shelves");
            modelBuilder.Entity<Shelf>().Property(s => s.Id).ValueGeneratedNever();
        }
    }

    private static DbContextOptions<ArrayContext> ArrayOptions(H2Connection connection)
        => new DbContextOptionsBuilder<ArrayContext>().UseH2(connection).Options;

    private sealed class MeasurementContext : DbContext
    {
        public MeasurementContext(DbContextOptions<MeasurementContext> options) : base(options)
        {
        }

        public DbSet<Measurement> Measurements => Set<Measurement>();
    }

    private static H2Connection Open(string name)
    {
        var connection = new H2Connection("jdbc:h2:mem:" + name + "_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        return connection;
    }

    private static DbContextOptions<MeasurementContext> Options(H2Connection connection)
        => new DbContextOptionsBuilder<MeasurementContext>().UseH2(connection).Options;
}
