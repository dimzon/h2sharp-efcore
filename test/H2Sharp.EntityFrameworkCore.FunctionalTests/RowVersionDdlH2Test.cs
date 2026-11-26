using H2Sharp.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace H2Sharp.EntityFrameworkCore;

public class RowVersionDdlH2Test(ITestOutputHelper output)
{
    [ConditionalFact]
    public void Row_version_column_gets_default_and_on_update_expressions()
    {
        using var context = new VersionContext();
        var script = context.Database.GenerateCreateScript();
        output.WriteLine(script);

        Assert.Contains("DEFAULT ", script);
        Assert.Contains("ON UPDATE ", script);
    }

    private class VersionContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2("Data Source=RowVersionDdl;Storage=Memory", b => b.ApplyConfiguration());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Versioned>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Version).IsRowVersion();
            });

        private class Versioned
        {
            public int Id { get; set; }
            public byte[] Version { get; set; } = null!;
        }
    }
}
