using System.Data.Common;
using H2Sharp.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Diagnostics;

#nullable enable

namespace H2Sharp.EntityFrameworkCore;

public class H2JoinedDeleteTests
{
    [Fact]
    public void Joined_delete_uses_a_key_subquery_on_the_reference()
        => VerifyJoinedDelete(checkPort: false);

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void Joined_delete_removes_only_matching_rows_on_the_port()
        => VerifyJoinedDelete(checkPort: true);

    private static void VerifyJoinedDelete(bool checkPort)
    {
        using var connection = new H2Connection("jdbc:h2:mem:joined_delete_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        var recorder = new Recorder();
        using var context = new DeleteContext(new DbContextOptionsBuilder<DeleteContext>()
            .UseH2(connection).AddInterceptors(recorder).Options);
        context.Database.EnsureCreated();
        context.Rows.AddRange(new Row { Id = 1 }, new Row { Id = 2 });
        context.Matches.Add(new Match { Id = 1, RowId = 2 });
        context.SaveChanges();

        // The reference must execute the generated DELETE and leave exactly row 1. The separate
        // port test remains red while the port cannot read IN with a subquery.
        var portError = Record.Exception(() => context.Rows.Join(context.Matches, r => r.Id, m => m.RowId, (r, m) => r).ExecuteDelete());
        Assert.NotNull(recorder.Sql);
        Assert.DoesNotContain("USING", recorder.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SELECT", recorder.Sql, StringComparison.OrdinalIgnoreCase);

        if (H2ReferenceOracle.IsAvailable)
        {
            var reference = H2ReferenceOracle.Run($"""
                {context.Database.GenerateCreateScript()}
                INSERT INTO "Rows" VALUES (1), (2);
                INSERT INTO "Matches" VALUES (1, 2);
                {recorder.Sql};
                SELECT 1 / CASE WHEN COUNT(*) = 1 AND MIN("Id") = 1 THEN 1 ELSE 0 END FROM "Rows";
                """);
            Assert.True(reference.Accepted, reference.Output);
        }

        if (checkPort)
        {
            Assert.Null(portError);
            Assert.Equal(new[] { 1 }, context.Rows.Select(r => r.Id).ToArray());
        }
    }

    private sealed class Recorder : DbCommandInterceptor
    {
        public string? Sql { get; private set; }

        public override DbCommand CommandInitialized(CommandEndEventData eventData, DbCommand result)
        {
            if (result.CommandText.StartsWith("DELETE", StringComparison.Ordinal))
            {
                Sql = result.CommandText;
            }
            return result;
        }
    }

    private sealed class Row { public int Id { get; set; } }
    private sealed class Match
    {
        public int Id { get; set; }
        public int RowId { get; set; }
    }
    private sealed class DeleteContext(DbContextOptions<DeleteContext> options) : DbContext(options)
    {
        public DbSet<Row> Rows => Set<Row>();
        public DbSet<Match> Matches => Set<Match>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Row>().ToTable("Rows").Property(r => r.Id).ValueGeneratedNever();
            modelBuilder.Entity<Match>().ToTable("Matches").Property(m => m.Id).ValueGeneratedNever();
        }
    }
}
