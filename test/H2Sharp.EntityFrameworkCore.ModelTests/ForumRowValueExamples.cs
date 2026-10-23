using Microsoft.EntityFrameworkCore;

using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The comparisons of rows: a pair of columns is compared as one value, and a list from a
///     subquery is asked about membership.
/// </summary>
public class ForumRowValueExamples
{
    [Fact]
    public void A_pair_of_columns_is_compared_as_one_value()
    {
        using var database = ForumDatabase.Filled(nameof(A_pair_of_columns_is_compared_as_one_value));
        using var context = database.Context();

        var equal = context.TopicTags.Where(t => EF.Functions.Equal(ValueTuple.Create(t.TopicId, t.TagId), ValueTuple.Create(1, 1))).ToList();
        var greater = context.TopicTags.Where(t => EF.Functions.GreaterThan(ValueTuple.Create(t.TopicId, t.TagId), ValueTuple.Create(1, 1))).ToList();
        var greaterOrEqual = context.TopicTags.Where(t => EF.Functions.GreaterThanOrEqual(ValueTuple.Create(t.TopicId, t.TagId), ValueTuple.Create(1, 2))).ToList();
        var less = context.TopicTags.Where(t => EF.Functions.LessThan(ValueTuple.Create(t.TopicId, t.TagId), ValueTuple.Create(1, 1))).ToList();
        var lessOrEqual = context.TopicTags.Where(t => EF.Functions.LessThanOrEqual(ValueTuple.Create(t.TopicId, t.TagId), ValueTuple.Create(1, 1))).ToList();

        Assert.Single(equal);
        Assert.Equal(6, greater.Count);
        Assert.Equal(6, greaterOrEqual.Count);
        Assert.Empty(less);
        Assert.Single(lessOrEqual);
    }

    [Fact]
    public void Membership_in_a_subquery_is_asked_by_any()
    {
        using var database = ForumDatabase.Filled(nameof(Membership_in_a_subquery_is_asked_by_any));
        using var context = database.Context();

        var questionOfAda = context.Posts
            .Where(p => p.AuthorId == context.Users.Where(u => u.UserName == "ada").Select(u => u.Id).First())
            .Select(p => p.TopicId)
            .Distinct();

        var topics = context.Topics
            .Where(t => questionOfAda.Contains(t.Id))
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.NotEmpty(topics);
        Assert.Contains("What a compiler does with the source", topics);
    }
}
