using System.Text;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Two tables tied by a foreign key: the navigation is read as a join, the books of an author
///     come with the author, and a joined update is reduced to a subquery H2 2.5 accepts.
/// </summary>
public class ForumRelationshipExamples
{
    [Fact]
    public void Books_find_their_author_through_a_join()
    {
        using var connection = Open(nameof(Books_find_their_author_through_a_join));
        using var context = new LibraryContext(Options(connection));
        context.Database.EnsureCreated();
        Seed(context);

        var authors = context.Books.Include(b => b.Author).OrderBy(b => b.Id).Select(b => b.Author.Name).ToList();

        Assert.Equal(["Ada", "Ada", "Grace"], authors);
    }

    [Fact]
    public void An_author_comes_with_the_books()
    {
        using var connection = Open(nameof(An_author_comes_with_the_books));
        using var context = new LibraryContext(Options(connection));
        context.Database.EnsureCreated();
        Seed(context);

        var authors = context.Authors.Include(a => a.Books).OrderBy(a => a.Id).ToList();

        Assert.Equal(["Ada", "Grace", "Linus"], authors.Select(a => a.Name));
        Assert.Equal([[1, 2], [3], []], authors.Select(a => a.Books.Select(b => b.Id).ToList()));
    }

    [Fact]
    public void A_navigation_in_the_predicate_becomes_a_join()
    {
        using var connection = Open(nameof(A_navigation_in_the_predicate_becomes_a_join));
        using var context = new LibraryContext(Options(connection));
        context.Database.EnsureCreated();
        Seed(context);

        var titles = context.Books
            .Where(b => b.Author.Name == "Ada")
            .OrderBy(b => b.Id)
            .Select(b => b.Title)
            .ToList();

        Assert.Equal(["Compiler", "Notes"], titles);
    }

    [Fact]
    public void An_author_without_books_counts_zero()
    {
        using var connection = Open(nameof(An_author_without_books_counts_zero));
        using var context = new LibraryContext(Options(connection));
        context.Database.EnsureCreated();
        Seed(context);

        var counts = context.Authors.OrderBy(a => a.Id).Select(a => a.Books.Count).ToList();

        Assert.Equal([2, 1, 0], counts);
    }

    [Fact]
    public void A_joined_update_is_reduced_to_a_subquery()
    {
        using var connection = Open(nameof(A_joined_update_is_reduced_to_a_subquery));
        var recorder = new SqlRecorder();
        using var context = new LibraryContext(Options(connection, recorder));
        context.Database.EnsureCreated();
        Seed(context);

        var changed = context.Books
            .Where(b => b.Author.Name == "Ada")
            .ExecuteUpdate(setters => setters.SetProperty(b => b.Shelf, "top"));

        Assert.Equal(2, changed);
        Assert.Equal(
            ["top", "top", "C"],
            context.Books.OrderBy(b => b.Id).Select(b => b.Shelf).ToList());

        // H2 2.5 rejects UPDATE ... FROM, so the statement has to be accepted by the reference
        // in the form the provider generated it.
        var update = recorder.Commands.Single(sql => sql.StartsWith("UPDATE", StringComparison.Ordinal));
        Assert.Contains("WHERE EXISTS (", update);
        var rows = H2ReferenceOracle.Query(
            Setup(context) + update + ";",
            "SELECT \"Shelf\" FROM \"Books\" ORDER BY \"Id\"");
        Assert.Equal(["top", "top", "C"], rows.Select(row => row[0]));
    }

    private static H2Connection Open(string name)
    {
        var connection = new H2Connection("jdbc:h2:mem:" + name + "_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        return connection;
    }

    private static DbContextOptions<LibraryContext> Options(H2Connection connection, SqlRecorder? recorder = null)
    {
        var builder = new DbContextOptionsBuilder<LibraryContext>().UseH2(connection);
        if (recorder != null)
        {
            builder.AddInterceptors(recorder);
        }

        return builder.Options;
    }

    private static void Seed(LibraryContext context)
    {
        var ada = new Author { Id = 1, Name = "Ada" };
        var grace = new Author { Id = 2, Name = "Grace" };
        var linus = new Author { Id = 3, Name = "Linus" };
        context.Authors.AddRange(ada, grace, linus);
        context.Books.AddRange(
            new Book { Id = 1, Title = "Compiler", Shelf = "A", Author = ada },
            new Book { Id = 2, Title = "Notes", Shelf = "B", Author = ada },
            new Book { Id = 3, Title = "COBOL", Shelf = "C", Author = grace });
        context.SaveChanges();
    }

    private static string Setup(LibraryContext context)
    {
        var script = new StringBuilder(context.Database.GenerateCreateScript());
        script.AppendLine("INSERT INTO \"Authors\" (\"Id\", \"Name\") VALUES (1, 'Ada'), (2, 'Grace'), (3, 'Linus');");
        script.AppendLine(
            "INSERT INTO \"Books\" (\"Id\", \"Title\", \"Shelf\", \"AuthorId\") "
            + "VALUES (1, 'Compiler', 'A', 1), (2, 'Notes', 'B', 1), (3, 'COBOL', 'C', 2);");
        return script.ToString();
    }

    private sealed class Author
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public List<Book> Books { get; set; } = [];
    }

    private sealed class Book
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string? Shelf { get; set; }

        public int AuthorId { get; set; }

        public Author Author { get; set; } = null!;
    }

    private sealed class LibraryContext(DbContextOptions<LibraryContext> options) : DbContext(options)
    {
        public DbSet<Author> Authors => Set<Author>();

        public DbSet<Book> Books => Set<Book>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>().ToTable("Authors");
            modelBuilder.Entity<Book>().ToTable("Books");
            modelBuilder.Entity<Author>().Property(a => a.Id).ValueGeneratedNever();
            modelBuilder.Entity<Book>().Property(b => b.Id).ValueGeneratedNever();
        }
    }
}
