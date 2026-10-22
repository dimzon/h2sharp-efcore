using H2Sharp.EntityFrameworkCore.TestUtilities;

#nullable enable

namespace H2Sharp.EntityFrameworkCore;

public class H2ByteSubstringTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Every_byte_is_read_with_substring(bool first)
    {
        using var connection = new H2Connection("jdbc:h2:mem:byte_substring_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new ByteContext(new DbContextOptionsBuilder<ByteContext>().UseH2(connection).Options);
        context.Database.EnsureCreated();

        var bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        context.Bytes.AddRange(Enumerable.Range(0, 256).Select(i => new ByteRow
        {
            Id = i,
            Offset = first ? 0 : i,
            Data = first ? [(byte)i] : bytes
        }));
        context.SaveChanges();

        var query = first
            ? context.Bytes.Select(b => new { b.Id, Value = b.Data.First() })
            : context.Bytes.Select(b => new { b.Id, Value = b.Data[b.Offset] });
        var sql = query.ToQueryString();
        Assert.Contains("substring(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bitand(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("get_byte", sql, StringComparison.OrdinalIgnoreCase);

        var values = query.OrderBy(b => b.Id).ToList();
        Assert.Equal(bytes, values.Select(b => b.Value));

        if (H2ReferenceOracle.IsAvailable)
        {
            var inserts = string.Join(Environment.NewLine, Enumerable.Range(0, 256).Select(i =>
                $"INSERT INTO \"Bytes\" VALUES ({i}, {(first ? 0 : i)}, X'{Convert.ToHexString(first ? [(byte)i] : bytes)}');"));
            // Check the generated SELECT's values on the JVM, not just its syntax. A wrong value
            // makes the denominator zero and fails the reference run.
            var script = $"""
                CREATE TABLE "Bytes" ("Id" INTEGER PRIMARY KEY, "Offset" INTEGER NOT NULL, "Data" VARBINARY NOT NULL);
                {inserts}
                SELECT 1 / CASE WHEN q."Id" = q."Value" THEN 1 ELSE 0 END FROM ({sql}) q;
                """;
            var reference = H2ReferenceOracle.Run(script);
            Assert.True(reference.Accepted, reference.Output);
        }
    }

    [Fact]
    public void Constant_and_parameter_indexes_keep_the_unsigned_byte()
    {
        using var connection = new H2Connection("jdbc:h2:mem:byte_index_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new ByteContext(new DbContextOptionsBuilder<ByteContext>().UseH2(connection).Options);
        context.Database.EnsureCreated();
        context.Bytes.Add(new ByteRow { Id = 1, Data = [0, 127, 128, 255] });
        context.SaveChanges();

        Assert.Equal((byte)255, context.Bytes.Select(b => b.Data[3]).Single());
        var offset = 2;
        Assert.Equal((byte)128, context.Bytes.Select(b => b.Data[offset]).Single());
        Assert.Equal(new byte[] { 128 }, context.Bytes.Where(b => b.Data[offset] == 128).Select(b => b.Data[offset]).ToArray());
    }

    private sealed class ByteRow
    {
        public int Id { get; set; }
        public int Offset { get; set; }
        public byte[] Data { get; set; } = [];
    }

    private sealed class ByteContext(DbContextOptions<ByteContext> options) : DbContext(options)
    {
        public DbSet<ByteRow> Bytes => Set<ByteRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ByteRow>().ToTable("Bytes");
            modelBuilder.Entity<ByteRow>().Property(b => b.Id).ValueGeneratedNever();
        }
    }
}
