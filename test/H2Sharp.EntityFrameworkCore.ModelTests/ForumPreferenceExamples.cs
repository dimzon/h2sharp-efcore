using System.Linq.Expressions;
using System.Text.Json;
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Subscription and delivery preferences are application data, queried through EF collections.
/// </summary>
public class ForumPreferenceExamples
{
    [Fact]
    public void Subscribers_are_found_by_forum_and_optional_notification_level()
    {
        using var context = Context();
        Verify(context, p => p.Forums.Contains(2), [1, 2]);
        Verify(context, p => !p.Forums.Contains(2), [3]);
        Verify(context, p => p.Levels.Contains(null), [1]);
        Verify(context, p => !p.Levels.Contains(null), [2, 3]);
        int? level = null;
        Verify(context, p => p.Levels.Contains(level), [1]);
        level = 2;
        Verify(context, p => p.Levels.Contains(level), [1, 2]);
    }

    [Fact]
    public void Delivery_screen_reads_collection_counts_and_first_subscriptions()
    {
        using var context = Context();
        Verify(context, p => p.Forums.Length == 3, [1]);
        Verify(context, p => p.Forums.Count() == 1, [2, 3]);
        Verify(context, p => p.Forums.Any(), [1, 2, 3]);
        Verify(context, p => p.Forums[0] == 2, [2]);
        var index = 1;
        Verify(context, p => p.Forums.Length > index && p.Forums[index] == 2, [1]);
        var subscribed = new[] { 2 };
        Verify(context, p => p.Forums.SequenceEqual(subscribed), [2]);
        Verify(context, p => p.Forums.Skip(1).Take(1).Contains(2), [1]);
        Verify(context, p => p.Forums.Take(2).Contains(3), [3]);
        Verify(context, p => p.Forums.Skip(1).Contains(3), [1]);
        var extra = new[] { 9 };
        Verify(context, p => p.Forums.Concat(extra).Contains(9), [1, 2, 3]);
        Verify(context, p => p.Forums.Take(0).Contains(1), []);
        Verify(context, p => p.Forums.Take(-1).Contains(1), []);
        Verify(context, p => p.Forums.Skip(50).Contains(1), []);
        Verify(context, p => p.Forums.Skip(-1).Contains(2), [1, 2]);
        Verify(context, p => p.Forums.Skip(1).Take(50).Contains(3), [1]);
    }

    [Fact]
    public void Converted_role_preferences_are_passed_as_real_query_parameters()
    {
        using var context = Context();
        var levels = new HashSet<int> { 1, 3 };
        Verify(context, p => levels.Contains(p.Id), [1, 3]);
        IEnumerable<int> streamed = Enumerable.Range(1, 3).Where(i => i % 2 != 0);
        Verify(context, p => streamed.Contains(p.Id), [1, 3]);
        var names = new List<string> { "ada", "grace" };
        Verify(context, p => names.Contains(p.Name), [1, 2]);
    }

    [Fact]
    public void Preferences_are_stored_by_ef_without_losing_json_or_binary_content()
    {
        using var connection = new H2Connection("jdbc:h2:mem:plain_preferences_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new PlainPreferenceContext(new DbContextOptionsBuilder<PlainPreferenceContext>().UseH2(connection).Options);
        context.Database.EnsureCreated();
        context.Settings.Add(new PlainPreference { UserId = 1, Json = JsonDocument.Parse("{\"theme\":\"dark\",\"levels\":[1,null,2]}"), Token = [0, 127, 128, 255] });
        context.SaveChanges();
        context.ChangeTracker.Clear();
        var settings = context.Settings.Single(s => s.UserId == 1);
        Assert.Equal("dark", settings.Json.RootElement.GetProperty("theme").GetString());
        Assert.Equal(new byte[] { 0, 127, 128, 255 }, settings.Token);
        Assert.Equal((byte)255, context.Settings.Where(s => s.UserId == 1).Select(s => s.Token[3]).Single());
        Assert.Equal((byte)0, context.Settings.Select(s => s.Token.First()).Single());
    }

    [Fact]
    public void Document_paths_have_no_form_in_h2()
    {
        using var connection = new H2Connection("jdbc:h2:mem:plain_json_path_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new PlainPreferenceContext(new DbContextOptionsBuilder<PlainPreferenceContext>().UseH2(connection).Options);

        // H2 2.5 stores JSON as text: there is no operator that walks into it, and no
        // function that counts the elements of a JSON array. The provider refuses instead
        // of answering with SQL the reference rejects.
        var traversal = Assert.Throws<NotSupportedException>(
            () => context.Settings.Where(s => s.Json.RootElement.GetProperty("theme").GetString() == "dark").ToList());
        var length = Assert.Throws<NotSupportedException>(
            () => context.Settings.Select(s => s.Json.RootElement.GetArrayLength()).ToList());

        Assert.All(
            [traversal, length],
            e => Assert.Contains("JSON", e.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void Binary_token_length_is_read_with_length()
    {
        using var connection = new H2Connection("jdbc:h2:mem:token_length_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new PlainPreferenceContext(new DbContextOptionsBuilder<PlainPreferenceContext>().UseH2(connection).Options);
        context.Database.EnsureCreated();
        context.Settings.Add(new PlainPreference { UserId = 1, Json = JsonDocument.Parse("{}"), Token = [0, 127, 128, 255] });
        context.SaveChanges();
        Assert.Equal(4, context.Settings.Where(s => s.UserId == 1).Select(s => s.Token.Length).Single());
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void Binary_token_is_searched_for_a_marker_byte()
    {
        using var connection = new H2Connection("jdbc:h2:mem:token_marker_" + Guid.NewGuid().ToString("N"));
        connection.Open();
        using var context = new PlainPreferenceContext(new DbContextOptionsBuilder<PlainPreferenceContext>().UseH2(connection).Options);
        context.Database.EnsureCreated();
        context.Settings.Add(new PlainPreference { UserId = 1, Json = JsonDocument.Parse("{}"), Token = [0, 127, 128, 255] });
        context.SaveChanges();
        var query = context.Settings.Where(s => s.Token.Contains((byte)128)).Select(s => s.UserId);
        using var command = query.CreateDbCommand();
        var rows = H2ReferenceOracle.Query(context.Database.GenerateCreateScript() + """
            INSERT INTO "Settings" VALUES (1, JSON '{}', X'007F80FF');
            """, ReferenceForum.Sql(command));
        Assert.Equal(new[] { "1" }, rows.Select(row => row[0]));
        Assert.Equal(new[] { 1 }, query.ToArray());
    }

    private static PreferenceContext Context()
        => new(new DbContextOptionsBuilder<PreferenceContext>().UseH2("jdbc:h2:mem:preferences_" + Guid.NewGuid().ToString("N")).Options);

    private static void Verify(PreferenceContext context, Expression<Func<Preference, bool>> predicate, int[] expected)
    {
        var query = context.Preferences.Where(predicate).OrderBy(p => p.Id).Select(p => p.Id);
        using var command = query.CreateDbCommand();
        var setup = context.Database.GenerateCreateScript() + """
            INSERT INTO "Preferences" VALUES (1, 'ada', ARRAY[1,2,3], ARRAY[1,NULL,2]);
            INSERT INTO "Preferences" VALUES (2, 'grace', ARRAY[2], ARRAY[2]);
            INSERT INTO "Preferences" VALUES (3, 'linus', ARRAY[3], ARRAY[]);
            """;
        var rows = H2ReferenceOracle.Query(setup, ReferenceForum.Sql(command));
        Assert.Equal(expected.Select(i => i.ToString()), rows.Select(row => row[0]));
    }

    private sealed class Preference
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int[] Forums { get; set; } = [];
        public int?[] Levels { get; set; } = [];
    }

    private sealed class PreferenceContext(DbContextOptions<PreferenceContext> options) : DbContext(options)
    {
        public DbSet<Preference> Preferences => Set<Preference>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Preference>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<Preference>().ToTable("Preferences");
        }
    }

    private sealed class PlainPreference
    {
        public int UserId { get; set; }
        public JsonDocument Json { get; set; } = null!;
        public byte[] Token { get; set; } = [];
    }
    private sealed class PlainPreferenceContext(DbContextOptions<PlainPreferenceContext> options) : DbContext(options)
    {
        public DbSet<PlainPreference> Settings => Set<PlainPreference>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlainPreference>().HasKey(p => p.UserId);
            modelBuilder.Entity<PlainPreference>().Property(p => p.UserId).ValueGeneratedNever();
        }
    }
}
