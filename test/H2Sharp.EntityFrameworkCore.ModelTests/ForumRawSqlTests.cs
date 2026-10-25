using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are asked for with a raw statement, and the changes are made in a
///     transaction.
/// </summary>
public class ForumRawSqlTests
{
    [Fact]
    public void The_rows_are_asked_for_with_a_raw_statement()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_raw_statement));
        using var context = database.Context();

        var topics = context.Topics
            .FromSqlRaw("SELECT * FROM \"Topics\" ORDER BY \"Id\"")
            .ToList();

        Assert.Equal(7, topics.Count);
        Assert.Equal("What a compiler does with the source", topics[0].Title);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_raw_statement_and_a_parameter()
    {
        using var database = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_raw_statement_and_a_parameter));
        using var context = database.Context();

        var topics = context.Topics
            .FromSqlRaw("SELECT * FROM \"Topics\" WHERE \"ForumId\" = {0}", 1)
            .ToList();

        Assert.Equal(3, topics.Count);
    }

    [Fact]
    public void A_row_is_written_with_a_raw_statement()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_written_with_a_raw_statement));
        using var context = database.Context();

        int written = context.Database.ExecuteSqlRaw(
            "INSERT INTO \"Tags\" (\"Name\") VALUES ('raw')");

        Assert.Equal(1, written);
        Assert.Equal(5, context.Tags.Count());
    }

    [Fact]
    public void A_row_is_written_with_a_raw_statement_and_a_parameter()
    {
        using var database = ForumDatabase.Filled(nameof(A_row_is_written_with_a_raw_statement_and_a_parameter));
        using var context = database.Context();

        int written = context.Database.ExecuteSqlRaw(
            "INSERT INTO \"Tags\" (\"Name\") VALUES ({0})",
            "raw");

        Assert.Equal(1, written);
        Assert.Equal(5, context.Tags.Count());
    }

    [Fact]
    public void The_change_is_kept_when_the_transaction_is_committed()
    {
        using var database = ForumDatabase.Filled(nameof(The_change_is_kept_when_the_transaction_is_committed));
        using var context = database.Context();

        // EF Core puts a savepoint inside the transaction, and the port reads it (dimzon/h2sharp#34).
        using var transaction = context.Database.BeginTransaction();

        var topic = context.Topics.OrderBy(t => t.Id).First();
        topic.Title = "A title of a committed change";
        context.SaveChanges();

        transaction.Commit();

        using var other = database.Context();
        Assert.Equal("A title of a committed change", other.Topics.OrderBy(t => t.Id).First().Title);
    }

    [Fact]
    public void The_change_is_thrown_away_when_the_transaction_is_rolled_back()
    {
        using var database = ForumDatabase.Filled(nameof(The_change_is_thrown_away_when_the_transaction_is_rolled_back));
        using var context = database.Context();

        using (var transaction = context.Database.BeginTransaction())
        {
            var topic = context.Topics.OrderBy(t => t.Id).First();
            topic.Title = "A title of a rolled back change";
            context.SaveChanges();

            transaction.Rollback();
        }

        using var other = database.Context();
        Assert.Equal("What a compiler does with the source", other.Topics.OrderBy(t => t.Id).First().Title);
    }
}
