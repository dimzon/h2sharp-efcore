using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The types of the provider: what goes into the database and what comes back.
/// </summary>
/// <remarks>
///     Every type the model carries is checked in both directions: the value that goes in comes
///     back unchanged.
/// </remarks>
public class ForumTypeTests
{
    [Fact]
    public void The_identity_key_is_given_out_by_the_database()
    {
        using var database = ForumDatabase.Empty(nameof(The_identity_key_is_given_out_by_the_database));
        using var context = database.Context();
        context.Database.EnsureCreated();

        var first = new Tag { Name = "one" };
        var second = new Tag { Name = "two" };
        context.Tags.AddRange(first, second);

        context.SaveChanges();

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
    }

    [Fact]
    public void The_enum_is_stored_as_a_string()
    {
        using var database = ForumDatabase.Filled(nameof(The_enum_is_stored_as_a_string));
        using var context = database.Context();

        var user = context.Users.Single(u => u.UserName == "ada");

        Assert.Equal(UserRole.Administrator, user.Role);
    }

    [Fact]
    public void The_enum_is_stored_as_a_string_and_not_as_a_number()
    {
        using var database = ForumDatabase.Filled(nameof(The_enum_is_stored_as_a_string_and_not_as_a_number));
        using var context = database.Context();

        var roles = context.Users.OrderBy(u => u.Id).Select(u => u.Role).ToList();

        Assert.Equal(
            [
                UserRole.Administrator,
                UserRole.Moderator,
                UserRole.Member,
                UserRole.Member,
                UserRole.Guest,
            ],
            roles);
    }

    [Fact]
    public void The_decimal_keeps_its_places()
    {
        using var database = ForumDatabase.Filled(nameof(The_decimal_keeps_its_places));
        using var context = database.Context();

        var user = context.Users.Single(u => u.UserName == "ada");

        Assert.Equal(12.50m, user.Balance);
    }

    [Fact]
    public void The_date_only_comes_back_as_it_went_in()
    {
        using var database = ForumDatabase.Filled(nameof(The_date_only_comes_back_as_it_went_in));
        using var context = database.Context();

        var user = context.Users.Single(u => u.UserName == "ada");

        Assert.Equal(new DateOnly(1990, 4, 12), user.BirthDate);
    }

    [Fact]
    public void The_time_only_comes_back_as_it_went_in()
    {
        using var database = ForumDatabase.Filled(nameof(The_time_only_comes_back_as_it_went_in));
        using var context = database.Context();

        var user = context.Users.Single(u => u.UserName == "ada");

        Assert.Equal(new TimeOnly(9, 30), user.PreferredTime);
    }

    [Fact]
    public void The_empty_date_and_time_come_back_as_nothing()
    {
        using var database = ForumDatabase.Filled(nameof(The_empty_date_and_time_come_back_as_nothing));
        using var context = database.Context();

        var user = context.Users.Single(u => u.UserName == "linus");

        Assert.Null(user.BirthDate);
        Assert.Null(user.PreferredTime);
    }

    [Fact]
    public void The_bytes_come_back_as_they_went_in()
    {
        using var database = ForumDatabase.Filled(nameof(The_bytes_come_back_as_they_went_in));
        using var context = database.Context();

        var user = context.Users.Single(u => u.UserName == "ada");

        Assert.Equal(new byte[] { 1, 2, 3 }, user.Avatar);
    }

    [Fact]
    public void The_empty_bytes_come_back_as_they_went_in()
    {
        using var database = ForumDatabase.Filled(nameof(The_empty_bytes_come_back_as_they_went_in));
        using var context = database.Context();

        Assert.Empty(context.Users.Single(u => u.UserName == "grace").Avatar);
    }

    [Fact]
    public void Rows_are_found_by_their_typed_values()
    {
        using var database = ForumDatabase.Filled(nameof(Rows_are_found_by_their_typed_values));
        using var context = database.Context();

        Assert.Equal("ada", context.Users.Single(u => u.BirthDate == new DateOnly(1990, 4, 12)).UserName);
        Assert.Equal("ada", context.Users.Single(u => u.PreferredTime == new TimeOnly(9, 30)).UserName);
        Assert.Equal("ada", context.Users.Single(u => u.Avatar == new byte[] { 1, 2, 3 }).UserName);
    }

    [Fact]
    public void The_date_and_the_time_come_back_as_they_went_in()
    {
        using var database = ForumDatabase.Filled(nameof(The_date_and_the_time_come_back_as_they_went_in));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();

        Assert.Equal(ForumData.Start.AddDays(10), topic.CreatedAt);
        Assert.Equal(ForumData.Now.AddHours(-3), topic.LastPostAt);
    }

    [Fact]
    public void The_string_keeps_its_length()
    {
        using var database = ForumDatabase.Filled(nameof(The_string_keeps_its_length));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();

        Assert.Equal("What a compiler does with the source", topic.Title);
    }

    [Fact]
    public void The_number_comes_back_as_it_went_in()
    {
        using var database = ForumDatabase.Filled(nameof(The_number_comes_back_as_it_went_in));
        using var context = database.Context();

        var topic = context.Topics.OrderBy(t => t.Id).First();

        Assert.Equal(1200, topic.ViewCount);
    }

    [Fact]
    public void The_two_columns_of_the_key_come_back_together()
    {
        using var database = ForumDatabase.Filled(nameof(The_two_columns_of_the_key_come_back_together));
        using var context = database.Context();

        var topicTag = context.TopicTags.OrderBy(t => t.TopicId).ThenBy(t => t.TagId).First();

        Assert.Equal(1, topicTag.TopicId);
        Assert.Equal(1, topicTag.TagId);
    }

    [Fact]
    public void The_types_of_the_model_are_the_types_of_H2()
    {
        using var database = ForumDatabase.Empty(nameof(The_types_of_the_model_are_the_types_of_H2));
        using var context = database.Context();
        context.Database.EnsureCreated();

        var types = context.Model
            .GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Select(p => p.GetColumnType())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        Assert.Contains("integer", types);
        Assert.Contains("varchar(100)", types);
        Assert.Contains("timestamp", types);
        Assert.Contains("date", types);
        Assert.Contains("time", types);
        Assert.Contains("boolean", types);
        Assert.Contains("numeric(18,2)", types);
        Assert.Contains("varbinary", types);
    }
}
