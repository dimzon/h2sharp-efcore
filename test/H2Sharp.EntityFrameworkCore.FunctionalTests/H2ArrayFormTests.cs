using System.Collections.Generic;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using H2Sharp.EntityFrameworkCore.TestUtilities;

#nullable enable

namespace H2Sharp.EntityFrameworkCore;

/// <summary>
/// The forms H2 2.5 takes for the operations over an array, and the ones it does not take
/// at all.
/// </summary>
/// <remarks>
/// The engine of this port does not read the type ARRAY yet, so a table with an array column
/// cannot be created against it, and the queries below cannot run. What they are about is the
/// statement the provider writes: the recorder has it whether the engine ran it or not.
/// </remarks>
public class H2ArrayFormTests
{
    private readonly IServiceProvider _serviceProvider = new ServiceCollection()
        .AddEntityFrameworkH2()
        .BuildServiceProvider();

    private sealed class Item
    {
        public int Id { get; set; }

        public int[] Numbers { get; set; } = [];

        public byte[] Blob { get; set; } = [];
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
    public void The_contains_of_an_array_is_written_the_way_H2_writes_it()
    {
        using var connection = new H2Connection("jdbc:h2:mem:array_contains");
        connection.Open();

        SqlRecorder recorder = new();
        using var context = new ItemContext(_serviceProvider, connection, recorder);

        // H2's containment: ARRAY_CONTAINS(array, item). The reference answers TRUE for
        // ARRAY_CONTAINS(ARRAY[1,2,3], 2) and FALSE for ARRAY_CONTAINS(ARRAY[1,2], NULL).
        // PostgreSQL's "arr @> ARRAY[item]" and the JSON "arr >> ARRAY[item]" are unknown to
        // H2 2.5.
        _ = Record.Exception(() => context.Items.Where(i => i.Numbers.Contains(5)).ToList());

        Assert.Contains(recorder.Commands, c => c.Contains("array_contains(", StringComparison.Ordinal));
        Assert.DoesNotContain(recorder.Commands, c => c.Contains("@>", StringComparison.Ordinal));
        Assert.DoesNotContain(recorder.Commands, c => c.Contains("array_position", StringComparison.Ordinal));
    }

    [Fact]
    public void The_place_of_an_element_in_an_array_is_not_translated()
    {
        using var connection = new H2Connection("jdbc:h2:mem:array_indexof");
        connection.Open();

        SqlRecorder recorder = new();
        using var context = new ItemContext(_serviceProvider, connection, recorder);

        // H2 2.5 has no function that says where an element is: the reference answers
        // "Function "ARRAY_POSITION" not found". ARRAY_CONTAINS answers whether the array
        // holds the element, not where. So the place is taken on the client, and the statement
        // the provider sends does not name a function that does not exist.
        var exception = Record.Exception(() => context.Items.Where(i => i.Numbers.IndexOf(5) > 0).ToList());

        Assert.NotNull(exception);
        Assert.DoesNotContain(recorder.Commands, c => c.Contains("array_position", StringComparison.Ordinal));
    }

    [Fact]
    public void The_byte_of_a_binary_is_taken_with_substring()
    {
        using var connection = new H2Connection("jdbc:h2:mem:array_byte");
        connection.Open();

        SqlRecorder recorder = new();
        using var context = new ItemContext(_serviceProvider, connection, recorder);

        // This model also has an ARRAY column, which is not available in the port yet. Inspect
        // the generated query without creating that table; live binary rows are tested separately.
        var sql = context.Items.Where(i => i.Blob[0] == 1).ToQueryString();

        Assert.Contains("substring(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bitand(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("get_byte", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(recorder.Commands, c => c.Contains("get_byte", StringComparison.Ordinal));
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

