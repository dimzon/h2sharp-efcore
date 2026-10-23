using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Named moods live in a domain type: the label goes in and the member comes back.
/// </summary>
/// <remarks>
///     H2 2.5 carries enumerations as a domain over strings, and the engine of this port
///     does not read CREATE TYPE yet, so the database below is made on the reference H2:
///     the provider builds the statement, and the reference answers which rows it gives.
/// </remarks>
public class ForumEnumExamples
{
    private const string Setup = """
        CREATE TYPE "MOOD" AS ENUM ('HAPPY', 'SAD');
        CREATE TABLE "Ranks" ("Id" INTEGER PRIMARY KEY, "Name" VARCHAR NOT NULL, "Mood" "MOOD" NOT NULL);
        INSERT INTO "Ranks" VALUES (1, 'ada', 'HAPPY'), (2, 'grace', 'SAD'), (3, 'linus', 'HAPPY');
        """;

    private enum Mood
    {
        Happy,
        Sad
    }

    [Fact]
    public void Moods_are_stored_in_a_named_domain_type_on_reference_h2()
    {
        using var context = Context();

        var happy = context.Ranks
            .Where(r => r.Mood == Mood.Happy)
            .OrderBy(r => r.Id)
            .Select(r => new { r.Name, r.Mood });
        using (var command = happy.CreateDbCommand())
        {
            Assert.Equal(
                [("ada", Mood.Happy), ("linus", Mood.Happy)],
                H2ReferenceOracle.Query(Setup, ReferenceForum.Sql(command)).Select(row => (row[0], Enum.Parse<Mood>(row[1]!, ignoreCase: true))).ToArray());
        }

        var ordered = context.Ranks.OrderBy(r => r.Mood).ThenBy(r => r.Id).Select(r => r.Name);
        using (var command = ordered.CreateDbCommand())
        {
            Assert.Equal(
                ["ada", "linus", "grace"],
                H2ReferenceOracle.Query(Setup, ReferenceForum.Sql(command)).Select(row => row[0]!).ToArray());
        }
    }

    private static RankContext Context()
        => new(new DbContextOptionsBuilder<RankContext>()
            .UseH2("jdbc:h2:mem:moods_" + Guid.NewGuid().ToString("N"), options => options.MapEnum<Mood>())
            .Options);

    private sealed class Rank
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Mood Mood { get; set; }
    }

    private sealed class RankContext(DbContextOptions<RankContext> options) : DbContext(options)
    {
        public DbSet<Rank> Ranks => Set<Rank>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rank>().ToTable("Ranks");
            modelBuilder.Entity<Rank>().Property(r => r.Id).ValueGeneratedNever();
        }
    }
}
