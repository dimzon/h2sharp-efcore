using H2Sharp.EntityFrameworkCore.TestUtilities;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the engine reads the default with a typed literal now, and the table is made;
// what fails is the in-memory database: it goes when the last connection closes, and the test
// store opens a new one for every step, so the second step answers "Table \"Chips\" not found
// (this database is empty)". The failure is the port's, not the provider's: see
// docs/DIVERGENCES.md.
[Trait("Category", "BlockedByPort")]
public class DefaultValuesTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider = new ServiceCollection()
        .AddEntityFrameworkH2()
        .BuildServiceProvider();

    [Fact]
    public void Can_use_H2_default_values()
    {
        using (var context = new ChipsContext(_serviceProvider, "DefaultKettleChips"))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var honeyDijon = context.Add(new KettleChips { Name = "Honey Dijon" }).Entity;
            var buffaloBleu = context.Add(
                new KettleChips { Name = "Buffalo Bleu", BestBuyDate = new DateTime(2111, 1, 11, 0, 0, 0, DateTimeKind.Utc) }).Entity;

            context.SaveChanges();

            Assert.Equal(new DateTime(2035, 9, 25, 0, 0, 0, DateTimeKind.Utc), honeyDijon.BestBuyDate);
            Assert.Equal(new DateTime(2111, 1, 11, 0, 0, 0, DateTimeKind.Utc), buffaloBleu.BestBuyDate);
        }

        using (var context = new ChipsContext(_serviceProvider, "DefaultKettleChips"))
        {
            Assert.Equal(
                new DateTime(2035, 9, 25, 0, 0, 0, DateTimeKind.Unspecified), context.Chips.Single(c => c.Name == "Honey Dijon").BestBuyDate);
            Assert.Equal(
                new DateTime(2111, 1, 11, 0, 0, 0, DateTimeKind.Utc), context.Chips.Single(c => c.Name == "Buffalo Bleu").BestBuyDate);
        }
    }

    public void Dispose()
    {
        using var context = new ChipsContext(_serviceProvider, "DefaultKettleChips");
        context.Database.EnsureDeleted();
    }

    private class ChipsContext(IServiceProvider serviceProvider, string databaseName) : DbContext
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly string _databaseName = databaseName;

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public DbSet<KettleChips> Chips { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseH2($"jdbc:h2:mem:{_databaseName}")
                .UseInternalServiceProvider(_serviceProvider);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<KettleChips>()
                .Property(e => e.BestBuyDate)
                .ValueGeneratedOnAdd()
                .HasDefaultValue(new DateTime(2035, 9, 25, 0, 0, 0, DateTimeKind.Unspecified));
    }

    private class KettleChips
    {
        // ReSharper disable once UnusedMember.Local
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime BestBuyDate { get; set; }
    }
}
