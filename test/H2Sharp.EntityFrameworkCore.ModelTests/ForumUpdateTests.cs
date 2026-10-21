using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are changed and removed.
/// </summary>
/// <remarks>
///     The rows go in and out the way an application does it: the row is read, changed, and
///     saved; the row is removed and the removal is saved.
/// </remarks>
public class ForumUpdateTests
{
    [Fact]
    public void A_row_is_changed_and_saved()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_changed_and_saved));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();
        topic.Title = "What a compiler does with the tokens";
        topic.ViewCount = 1300;

        context.SaveChanges();

        var changed = context.Topics.Single(t => t.Id == topic.Id);
        Assert.Equal("What a compiler does with the tokens", changed.Title);
        Assert.Equal(1300, changed.ViewCount);
    }

    [Fact]
    public void A_row_is_removed_and_saved()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_removed_and_saved));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();
        context.Topics.Remove(topic);

        context.SaveChanges();

        Assert.Equal(6, context.Topics.Count());
        Assert.Empty(context.Topics.Where(t => t.Id == topic.Id));
    }

    [Fact]
    public void A_row_is_added_and_saved()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_added_and_saved));
        using var context = database.Context();

        context.Topics.Add(new Topic
        {
            ForumId = 1,
            AuthorId = 1,
            Title = "A topic that is new",
            Body = "The body of the new topic.",
            CreatedAt = ForumData.Now,
            LastPostAt = ForumData.Now,
            ViewCount = 0,
        });

        context.SaveChanges();

        Assert.Equal(8, context.Topics.Count());
        Assert.Contains(context.Topics, t => t.Title == "A topic that is new");
    }

    [Fact]
    public void The_key_of_an_added_row_comes_back_from_the_database()
    {
        using var database = ForumDatabase.Filled(nameof(The_key_of_an_added_row_comes_back_from_the_database));
        using var context = database.Context();

        var tag = new Tag { Name = "benchmarks" };
        context.Tags.Add(tag);

        context.SaveChanges();

        Assert.True(tag.Id > 0);
    }

    [Fact]
    public void A_row_that_breaks_a_unique_index_is_refused()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_that_breaks_a_unique_index_is_refused));
        using var context = database.Context();

        context.Tags.Add(new Tag { Name = "compilers" });

        Assert.ThrowsAny<Exception>(() => context.SaveChanges());
    }

    [Fact]
    public void A_change_of_a_row_is_not_written_without_a_save()
    {
        using var database = ForumDatabase.Filled(nameof(A_change_of_a_row_is_not_written_without_a_save));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();
        topic.Title = "A title nobody saved";

        using var other = database.Context();
        Assert.Equal("What a compiler does with the source", other.Topics.Single(t => t.Id == topic.Id).Title);
    }

    [Fact]
    public void A_row_is_removed_by_a_query()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_removed_by_a_query));
        using var context = database.Context();

        int removed = context.Topics.Where(t => t.IsClosed).ExecuteDelete();

        Assert.Equal(2, removed);
        Assert.Equal(5, context.Topics.Count());
    }

    [Fact]
    public void A_row_is_changed_by_a_query()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_changed_by_a_query));
        using var context = database.Context();

        int changed = context.Topics.Where(t => t.ForumId == 1).ExecuteUpdate(s => s.SetProperty(t => t.ViewCount, 1));

        Assert.Equal(3, changed);
        Assert.Equal(3, context.Topics.Count(t => t.ViewCount == 1));
    }

    [Fact]
    public void A_change_of_a_row_is_undone()
    {
        using var database = ForumDatabase.Filled(nameof(A_change_of_a_row_is_undone));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();
        topic.Title = "A title nobody wanted";
        context.SaveChanges();

        context.ChangeTracker.Clear();

        Assert.Equal("A title nobody wanted", context.Topics.Single(t => t.Id == topic.Id).Title);
    }
}
