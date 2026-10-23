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

    private sealed class Shelf
    {
        public int Id { get; set; }

        public int[] Forums { get; set; } = [];
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
