namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The collections of the model that are not arrays: H2 stores them in array columns, and
///     the rows come back in the collection the model asks for.
/// </summary>
public class ForumCollectionExamples
{
    [Fact]
    public void A_list_comes_back_as_a_list()
    {
        using var connection = Open(nameof(A_list_comes_back_as_a_list));
        using var context = new CollectionContext(Options(connection));
        context.Database.EnsureCreated();

        context.Boxes.Add(new Box { Id = 1, Tags = [1, 2, 3] });
        context.SaveChanges();

        var box = context.Boxes.Single();

        Assert.Equal([1, 2, 3], box.Tags);
        Assert.Equal([1], context.Boxes.Where(b => b.Tags.Contains(2)).Select(b => b.Id).ToList());
    }

    [Fact]
    public void A_list_with_a_missing_entry_comes_back_as_it_went_in()
    {
        using var connection = Open(nameof(A_list_with_a_missing_entry_comes_back_as_it_went_in));
        using var context = new CollectionContext(Options(connection));
        context.Database.EnsureCreated();

        context.Boxes.Add(new Box { Id = 1, Levels = [1, null, 3] });
        context.SaveChanges();

        var box = context.Boxes.Single();

        Assert.Equal([1, null, 3], box.Levels);
    }

    [Fact]
    public void An_empty_list_comes_back_empty()
    {
        using var connection = Open(nameof(An_empty_list_comes_back_empty));
        using var context = new CollectionContext(Options(connection));
        context.Database.EnsureCreated();

        context.Boxes.Add(new Box { Id = 1, Tags = [] });
        context.SaveChanges();

        var box = context.Boxes.Single();

        Assert.Empty(box.Tags);
    }

    private static H2Connection Open(string name)
    {
        var connection = new H2Connection("jdbc:h2:mem:" + name + "_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        return connection;
    }

    private static DbContextOptions<CollectionContext> Options(H2Connection connection)
        => new DbContextOptionsBuilder<CollectionContext>().UseH2(connection).Options;

    private sealed class Box
    {
        public int Id { get; set; }

        public List<int> Tags { get; set; } = [];

        public List<int?>? Levels { get; set; } = [];
    }

    private sealed class CollectionContext(DbContextOptions<CollectionContext> options) : DbContext(options)
    {
        public DbSet<Box> Boxes => Set<Box>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Box>().ToTable("Boxes");
            modelBuilder.Entity<Box>().Property(b => b.Id).ValueGeneratedNever();
        }
    }
}
