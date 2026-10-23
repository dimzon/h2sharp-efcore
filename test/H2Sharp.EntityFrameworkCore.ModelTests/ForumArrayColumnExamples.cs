using System.Linq.Expressions;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Subscriptions live in array columns: membership, counts, positions and whole-array
///     comparisons are asked in the query and checked on H2 2.5.
/// </summary>
/// <remarks>
///     The engine of this port does not read the type ARRAY yet, so the database below is
///     made on the reference H2 instead: the provider builds the statement, and the reference
///     answers which rows it gives. No statement here is executed by the port.
/// </remarks>
public class ForumArrayColumnExamples
{
    private static readonly Shelf[] Shelves =
    [
        new() { Id = 1, Name = "ada", Forums = [1, 2, 3], Levels = [1, null, 2] },
        new() { Id = 2, Name = "grace", Forums = [2], Levels = [2] },
        new() { Id = 3, Name = "linus", Forums = [3], Levels = [] },
    ];

    [Fact]
    public void Subscribers_are_found_by_a_forum_in_their_array()
    {
        using var context = Context();
        Verify(context, s => s.Forums.Contains(2), [1, 2]);
        Verify(context, s => !s.Forums.Contains(2), [3]);
        Verify(context, s => s.Forums.Contains(s.Id), [1, 2, 3]);
        var forum = 3;
        Verify(context, s => s.Forums.Contains(forum), [1, 3]);
    }

    [Fact]
    public void Nullable_levels_tell_apart_missing_and_present_marks()
    {
        using var context = Context();
        Verify(context, s => s.Levels.Contains(null), [1]);
        Verify(context, s => !s.Levels.Contains(null), [2, 3]);
        int? level = null;
        Verify(context, s => s.Levels.Contains(level), [1]);
        level = 2;
        Verify(context, s => s.Levels.Contains(level), [1, 2]);
    }

    [Fact]
    public void Constant_lists_match_whole_rows()
    {
        using var context = Context();
        var wanted = new[] { 1, 3 };
        Verify(context, s => wanted.Contains(s.Id), [1, 3]);
        var names = new List<string> { "ada", "grace" };
        Verify(context, s => names.Contains(s.Name), [1, 2]);
    }

    [Fact]
    public void Inline_lists_match_whole_rows_on_reference_h2()
    {
        using var context = Context();

        // A list written in the query itself is expanded into the statement, the way
        // a parameter list is: H2 reads both IN lists and = ANY element checks.
        var byId = context.Shelves.Where(s => new[] { 1, 2 }.Contains(s.Id)).Select(s => s.Id);
        Assert.Equal(["1", "2"], ReferenceForum.Query(byId.OrderBy(id => id), Setup).Select(row => row[0]));

        var byName = context.Shelves.Where(s => new List<string> { "ada", "grace" }.Contains(s.Name)).Select(s => s.Name);
        Assert.Equal(["ada", "grace"], ReferenceForum.Query(byName.OrderBy(n => n), Setup).Select(row => row[0]));

        // A list built from variables is expanded with its placeholders: the reference
        // reads the array literal with parameters the same as with constants.
        var first = 1;
        var last = 2;
        var byVariables = context.Shelves.Where(s => new[] { first, last }.Contains(s.Id)).Select(s => s.Id);
        Assert.Equal(["1", "2"], ReferenceForum.Query(byVariables.OrderBy(id => id), Setup).Select(row => row[0]));
    }

    private const string Setup = """
        CREATE TABLE "Shelves" ("Id" INTEGER PRIMARY KEY, "Name" VARCHAR NOT NULL, "Forums" INTEGER ARRAY NOT NULL, "Levels" INTEGER ARRAY);
        INSERT INTO "Shelves" VALUES (1, 'ada', ARRAY[1,2,3], ARRAY[1,NULL,2]);
        INSERT INTO "Shelves" VALUES (2, 'grace', ARRAY[2], ARRAY[2]);
        INSERT INTO "Shelves" VALUES (3, 'linus', ARRAY[3], ARRAY[]);
        """;

    [Fact]
    public void Independent_array_is_unnested_into_rows_on_reference_h2()
    {
        using var context = Context();

        // A join against a list written in the query does not reference the outer row.
        var query = context.Shelves
            .Join(new[] { 1, 2 }, s => s.Id, x => x, (s, x) => s.Name)
            .OrderBy(n => n);
        Assert.Equal(
            ["ada", "grace"],
            ReferenceForum.Query(query, Setup).Select(row => row[0]));
    }

    [Fact]
    public void Any_asks_about_a_single_value()
    {
        using var context = Context();
        Verify(context, s => s.Forums.Any(f => f == 2), [1, 2]);
    }

    [Fact]
    public void Quantified_checks_without_an_h2_form_are_refused()
    {
        using var context = Context();

        // H2 supports UNNEST, but its arguments cannot refer to an outer row. These
        // queries require correlated UNNEST or LATERAL, which the reference rejects.
        var all = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Where(s => s.Forums.All(f => f > 0)).ToList());
        var greater = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Where(s => s.Forums.Any(f => f > 1)).ToList());
        var firstOrDefault = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Where(s => s.Levels.FirstOrDefault() == 2).ToList());
        var take = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Select(s => s.Forums.Take(2).ToList()).ToList());
        var flatten = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.SelectMany(s => s.Forums.Take(2)).ToList());
        var elementOrDefault = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Select(s => s.Forums.ElementAtOrDefault(1)).ToList());

        // A rejected correlation must identify the unsupported SQL form.
        Assert.All(
            [all, greater],
            e => Assert.Contains("UNNEST", e.Message, StringComparison.Ordinal));
        Assert.All(
            [take, firstOrDefault, flatten, elementOrDefault],
            e => Assert.Contains("LATERAL", e.Message, StringComparison.Ordinal));

        // A variable take builds the same outer apply with computed bounds.
        var takeCount = 2;
        var takeVariable = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Select(s => s.Forums.Take(takeCount).ToList()).ToList());
        Assert.Contains("LATERAL", takeVariable.Message, StringComparison.Ordinal);

        var unequal = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Where(s => s.Forums.Any(f => f != 2)).ToList());
        Assert.Contains("could not be translated", unequal.Message, StringComparison.Ordinal);

        // This concatenation requires UNNEST over the outer row's array column.
        var joinedInline = Assert.Throws<InvalidOperationException>(
            () => context.Shelves.Where(s => s.Forums.Concat(new[] { 9 }).Contains(9)).ToList());
        Assert.Contains("UNNEST", joinedInline.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Counts_lengths_and_first_positions_come_from_the_array()
    {
        using var context = Context();
        Verify(context, s => s.Forums.Length == 3, [1]);
        Verify(context, s => s.Forums.Count() == 1, [2, 3]);
        Verify(context, s => s.Forums.Any(), [1, 2, 3]);
        Verify(context, s => s.Levels.Length == s.Forums.Length, [1, 2]);
        Verify(context, s => s.Forums[0] == 2, [2]);
        var index = 1;
        Verify(context, s => s.Forums.Length > index && s.Forums[index] == 2, [1]);
        Verify(context, s => s.Forums.First() == 2, [2]);
        Verify(context, s => s.Forums.Skip(1).Contains(2), [1]);
    }

    [Fact]
    public void Whole_arrays_are_compared()
    {
        using var context = Context();
        var subscribed = new[] { 2 };
        Verify(context, s => s.Forums.SequenceEqual(subscribed), [2]);
        var literal = new[] { 1, 2, 3 };
        Verify(context, s => s.Forums.SequenceEqual(literal), [1]);
        var extra = new[] { 9 };
        Verify(context, s => s.Forums.Concat(extra).Contains(9), [1, 2, 3]);
    }

    [Fact]
    public void Lists_are_stored_as_arrays_on_reference_h2()
    {
        using var context = ListContext();
        VerifyList(context, s => s.Tags.Contains(2), [1, 2]);
        VerifyList(context, s => !s.Tags.Contains(2), [3]);
        VerifyList(context, s => s.Tags.Count == 1, [2, 3]);
        VerifyList(context, s => s.Tags[0] == 2, [2]);
        var wanted = new List<int> { 2 };
        VerifyList(context, s => s.Tags.SequenceEqual(wanted), [2]);
    }

    private const string ListSetup = """
        CREATE TABLE "ListShelves" ("Id" INTEGER PRIMARY KEY, "Name" VARCHAR NOT NULL, "Tags" INTEGER ARRAY NOT NULL);
        INSERT INTO "ListShelves" VALUES (1, 'ada', ARRAY[1,2,3]);
        INSERT INTO "ListShelves" VALUES (2, 'grace', ARRAY[2]);
        INSERT INTO "ListShelves" VALUES (3, 'linus', ARRAY[3]);
        """;

    private static ListShelfContext ListContext()
        => new(new DbContextOptionsBuilder<ListShelfContext>().UseH2("jdbc:h2:mem:listshelves_" + Guid.NewGuid().ToString("N")).Options);

    private static void VerifyList(ListShelfContext context, Expression<Func<ListShelf, bool>> predicate, int[] expected)
    {
        var query = context.Shelves.Where(predicate).OrderBy(s => s.Id).Select(s => s.Id);
        using var command = query.CreateDbCommand();
        var rows = H2ReferenceOracle.Query(ListSetup, ReferenceForum.Sql(command));
        Assert.Equal(expected.Select(i => i.ToString()), rows.Select(row => row[0]));
    }

    private sealed class ListShelf
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<int> Tags { get; set; } = [];
    }

    private sealed class ListShelfContext(DbContextOptions<ListShelfContext> options) : DbContext(options)
    {
        public DbSet<ListShelf> Shelves => Set<ListShelf>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ListShelf>().ToTable("ListShelves");
            modelBuilder.Entity<ListShelf>().Property(s => s.Id).ValueGeneratedNever();
        }
    }

    private static ShelfContext Context()
        => new(new DbContextOptionsBuilder<ShelfContext>().UseH2("jdbc:h2:mem:shelves_" + Guid.NewGuid().ToString("N")).Options);

    private static void Verify(ShelfContext context, Expression<Func<Shelf, bool>> predicate, int[] expected)
    {
        var query = context.Shelves.Where(predicate).OrderBy(s => s.Id).Select(s => s.Id);
        using var command = query.CreateDbCommand();
        var rows = H2ReferenceOracle.Query(Setup, ReferenceForum.Sql(command));
        Assert.Equal(expected.Select(i => i.ToString()), rows.Select(row => row[0]));
    }

    private sealed class Shelf
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int[] Forums { get; set; } = [];
        public int?[] Levels { get; set; } = [];
    }

    private sealed class ShelfContext(DbContextOptions<ShelfContext> options) : DbContext(options)
    {
        public DbSet<Shelf> Shelves => Set<Shelf>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Shelf>().ToTable("Shelves");
            modelBuilder.Entity<Shelf>().Property(s => s.Id).ValueGeneratedNever();
        }
    }
}
