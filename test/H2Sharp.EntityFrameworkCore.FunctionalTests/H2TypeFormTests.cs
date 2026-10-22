using System.Collections.Generic;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

using H2Sharp.EntityFrameworkCore.TestUtilities;

#nullable enable

namespace H2Sharp.EntityFrameworkCore;

/// <summary>
/// The forms H2 2.5 writes, in the places where the provider used to write the forms of
/// PostgreSQL. Every one of them was checked on the reference engine: the reference takes
/// the form the provider writes now and refuses the form it wrote before.
/// </summary>
public class H2TypeFormTests
{
    private readonly IServiceProvider _serviceProvider = new ServiceCollection()
        .AddEntityFrameworkH2()
        .BuildServiceProvider();

    private sealed class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int[] Numbers { get; set; } = [];

        public DateTimeOffset Moment { get; set; }

        public TimeSpan Span { get; set; }
    }

    /// <summary>
    ///     The same model without the array column: the engine of this port does not read
    ///     the array type yet, so a table with such a column cannot be created, and the
    ///     queries below need a table to ask about.
    /// </summary>
    private sealed class Plain
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public DateTime When { get; set; }
    }

    private class PlainContext(IServiceProvider serviceProvider, DbConnection connection, SqlRecorder? recorder = null)
        : DbContext
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public DbSet<Plain> Plains { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseH2(connection)
                .UseInternalServiceProvider(_serviceProvider);

            if (recorder is not null)
            {
                optionsBuilder.AddInterceptors(recorder);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Plain>().HasKey(e => e.Id);
    }

    private class ItemContext(IServiceProvider serviceProvider, DbConnection connection, SqlRecorder? recorder = null)
        : DbContext
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public DbSet<Item> Items { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseH2(connection)
                .UseInternalServiceProvider(_serviceProvider);

            if (recorder is not null)
            {
                optionsBuilder.AddInterceptors(recorder);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Item>().HasKey(e => e.Id);
    }

    [Fact]
    public void The_array_column_is_declared_the_way_H2_declares_it()
    {
        using var connection = new H2Connection("jdbc:h2:mem:type_form_array");
        connection.Open();

        using var context = new ItemContext(_serviceProvider, connection);

        // H2 writes the type of the elements and the word ARRAY after it: INTEGER ARRAY.
        // The brackets of PostgreSQL (integer[]) the reference does not take.
        var script = context.Database.GenerateCreateScript();

        Assert.Contains("\"Numbers\" integer ARRAY", script, StringComparison.Ordinal);
        Assert.DoesNotContain("integer[]", script, StringComparison.Ordinal);
    }

    [Fact]
    public void The_array_literal_is_written_the_way_H2_writes_it()
    {
        using var connection = new H2Connection("jdbc:h2:mem:type_form_array_literal");
        connection.Open();

        using var context = new ItemContext(_serviceProvider, connection);

        var mapping = context.GetService<IRelationalTypeMappingSource>()
            .FindMapping(typeof(int[]));

        Assert.NotNull(mapping);

        // No redundant cast after the brackets: the type of the array follows from its elements.
        Assert.Equal("ARRAY[1,2]", mapping!.GenerateSqlLiteral(new[] { 1, 2 }));
    }

    [Fact]
    public void The_timestamp_with_time_zone_literal_is_written_the_way_H2_writes_it()
    {
        using var connection = new H2Connection("jdbc:h2:mem:type_form_timestamptz");
        connection.Open();

        using var context = new ItemContext(_serviceProvider, connection);

        var mapping = context.GetService<IRelationalTypeMappingSource>()
            .FindMapping(typeof(DateTimeOffset));

        Assert.NotNull(mapping);

        // TIMESTAMPTZ is the short spelling of PostgreSQL, and the reference refuses it:
        // "Syntax error in SQL statement "SELECT TIMESTAMPTZ [*]'2020-01-01 00:00:00+00'"".
        var literal = mapping!.GenerateSqlLiteral(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.StartsWith("TIMESTAMP WITH TIME ZONE '", literal, StringComparison.Ordinal);
        Assert.DoesNotContain("TIMESTAMPTZ", literal, StringComparison.Ordinal);
    }

    [Fact]
    public void The_interval_literal_is_written_the_way_H2_writes_it()
    {
        using var connection = new H2Connection("jdbc:h2:mem:type_form_interval");
        connection.Open();

        using var context = new ItemContext(_serviceProvider, connection);

        var mapping = context.GetService<IRelationalTypeMappingSource>()
            .FindMapping(typeof(TimeSpan), storeTypeName: "interval day to second");

        Assert.NotNull(mapping);

        // H2 2.5 takes the interval literal with its qualifier -- INTERVAL '1 2:3:4' DAY TO
        // SECOND -- and refuses the bare one: "Cannot parse "INTERVAL" constant "1 2:3:4"".
        // The cast of the same text to the same type is the form the engine of this port reads,
        // and the reference gives the same value for either.
        var literal = mapping!.GenerateSqlLiteral(new TimeSpan(1, 2, 3, 4));

        Assert.Equal("CAST('1 02:03:04' AS INTERVAL DAY TO SECOND)", literal);
    }

    [Fact]
    public void Contains_is_translated_with_the_function_of_H2()
    {
        using var connection = new H2Connection("jdbc:h2:mem:type_form_contains");
        connection.Open();

        SqlRecorder recorder = new();
        using var context = new PlainContext(_serviceProvider, connection, recorder);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // H2 has no strpos: the function that finds a substring is LOCATE, and its arguments
        // are the other way round -- LOCATE(substring, string). EF asks the provider for the
        // translation when the pattern is not a constant, so the pattern here is a column.
        //
        // The statement is recorded whether the engine runs it or not, and the engine of this
        // port does not read the join the query needs.
        _ = Record.Exception(() => context.Plains.Where(p => p.Name.Contains(p.Name)).ToList());

        Assert.Contains(recorder.Commands, c => c.Contains("locate(", StringComparison.Ordinal));
        Assert.DoesNotContain(recorder.Commands, c => c.Contains("strpos(", StringComparison.Ordinal));
    }

    [Fact]
    public void The_parts_of_a_date_are_taken_by_the_functions_of_H2()
    {
        using var connection = new H2Connection("jdbc:h2:mem:type_form_date_parts");
        connection.Open();

        SqlRecorder recorder = new();
        using var context = new PlainContext(_serviceProvider, connection, recorder);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        // H2 has no date_part: the part of a date is taken by a function of its own.
        _ = context.Plains.Where(p => p.When.Year == 2020).ToList();

        Assert.Contains(recorder.Commands, c => c.Contains("year(", StringComparison.Ordinal));
        Assert.DoesNotContain(recorder.Commands, c => c.Contains("date_part(", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Records the statements the provider sends, so that a test can say what the
    ///     provider wrote without waiting for the engine to run it.
    /// </summary>
    private sealed class SqlRecorder : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override DbCommand CommandInitialized(CommandEndEventData eventData, DbCommand result)
        {
            Commands.Add(result.CommandText);
            return result;
        }
    }
}
