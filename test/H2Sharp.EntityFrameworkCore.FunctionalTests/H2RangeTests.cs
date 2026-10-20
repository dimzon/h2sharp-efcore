using System.Collections.Generic;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using H2Sharp.EntityFrameworkCore.TestUtilities;

#nullable enable

namespace H2Sharp.EntityFrameworkCore;

/// <summary>
/// The range table of H2: <c>SYSTEM_RANGE(min, max[, step])</c>, a table function that gives a
/// row per number of the range.
/// </summary>
/// <remarks>
/// The engine reads it, and the provider can ask for it: EF Core sends the statement of
/// <see cref="FromSqlRaw(string)"/> as it is, and the statement is one the engine reads.
/// </remarks>
public class H2RangeTests
{
    private readonly IServiceProvider _serviceProvider = new ServiceCollection()
        .AddEntityFrameworkH2()
        .BuildServiceProvider();

    private sealed class RangeRow
    {
        public long X { get; set; }
    }

    private class RangeContext(IServiceProvider serviceProvider, DbConnection connection) : DbContext
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public DbSet<RangeRow> RangeRows { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseH2(connection)
                .UseInternalServiceProvider(_serviceProvider);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<RangeRow>().HasNoKey();
    }

    [Fact]
    public void The_range_table_gives_a_row_per_number()
    {
        using var connection = new H2Connection("jdbc:h2:mem:range_ef");
        connection.Open();

        using var context = new RangeContext(_serviceProvider, connection);

        List<RangeRow> rows = context.RangeRows.FromSqlRaw("SELECT * FROM SYSTEM_RANGE(1, 10)").ToList();

        Assert.Equal(10, rows.Count);
        Assert.Equal(1, rows[0].X);
        Assert.Equal(10, rows[9].X);
    }

    [Fact]
    public void The_range_table_can_be_asked_for_a_step()
    {
        using var connection = new H2Connection("jdbc:h2:mem:range_ef_step");
        connection.Open();

        using var context = new RangeContext(_serviceProvider, connection);

        List<RangeRow> rows = context.RangeRows.FromSqlRaw("SELECT * FROM SYSTEM_RANGE(1, 10, 2)").ToList();

        // The step is added to the start, and the range stops at the end: 1, 3, 5, 7, 9.
        Assert.Equal(5, rows.Count);
        Assert.Equal(1, rows[0].X);
        Assert.Equal(3, rows[1].X);
        Assert.Equal(9, rows[4].X);
    }
}
