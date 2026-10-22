using H2Sharp.EntityFrameworkCore.ValueGeneration;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Applications can allocate durable keys before SaveChanges with a sequence block or GUID.
/// </summary>
public class ForumKeyGenerationExamples
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Draft_keys_are_allocated_before_save_and_survive_reload(bool async)
    {
        using var connection = new H2Connection("jdbc:h2:mem:draft_keys_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        var options = new DbContextOptionsBuilder<DraftContext>().UseH2(connection).Options;
        using var context = new DraftContext(options);
        context.Database.EnsureCreated();
        var drafts = Enumerable.Range(0, 25).Select(i => new Draft { Title = "Draft " + i }).ToArray();
        foreach (var draft in drafts)
        {
            if (async) await context.AddAsync(draft);
            else context.Add(draft);
            Assert.True(draft.Id > 0);
            Assert.NotEqual(Guid.Empty, draft.PublicId);
        }
        Assert.Equal(25, drafts.Select(d => d.Id).Distinct().Count());
        Assert.Equal(25, drafts.Select(d => d.PublicId).Distinct().Count());
        if (async) await context.SaveChangesAsync();
        else context.SaveChanges();
        using var reloaded = new DraftContext(options);
        Assert.Equal(drafts.OrderBy(d => d.Id).Select(d => d.PublicId),
            reloaded.Drafts.OrderBy(d => d.Id).Select(d => d.PublicId).ToArray());
    }

    private sealed class Draft
    {
        public long Id { get; set; }
        public Guid PublicId { get; set; }
        public string Title { get; set; } = "";
    }
    private sealed class DraftContext(DbContextOptions<DraftContext> options) : DbContext(options)
    {
        public DbSet<Draft> Drafts => Set<Draft>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Draft>().Property(d => d.Id).UseHiLo("DraftKeys");
            modelBuilder.Entity<Draft>().Property(d => d.PublicId).ValueGeneratedOnAdd()
                .HasValueGenerator<H2SequentialGuidValueGenerator>();
        }
    }
}
