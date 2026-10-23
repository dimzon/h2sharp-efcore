/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The rows of the forum are gathered into one value.
/// </summary>
/// <remarks>
///     <para>
///         The gathering takes an order of its own and a filter of its own, and two gatherings
///         in one query may take different ones: the bodies of the answers of one author, the
///         longest first, and the bodies of the visible answers, the shortest first.
///     </para>
///     <para>
///         The reference H2 2.5 takes the order of the rows of a gathering in a
///         <c>WITHIN GROUP (ORDER BY …)</c> clause after the parentheses, and the filter of the
///         rows in a <c>FILTER (WHERE …)</c> clause after that. The engine does not read the
///         first of them yet, so the test that needs them is marked as its gap.
///     </para>
/// </remarks>
public class ForumAggregateTests
{
    [Fact]
    public void The_answers_are_gathered_into_one_text()
    {
        using var database = ForumDatabase.Filled(nameof(The_answers_are_gathered_into_one_text));
        using var context = database.Context();

        // The separator is one that no body of the forum contains, so that the parts of the
        // gathered text can be told apart.
        string gathered = string.Join(" | ", context.Posts.Where(p => p.AuthorId == 1).Select(p => p.Body));

        // The gathering has no order of its own, so the test does not lean on the order of the
        // rows: it asks whether every body of the author is among the parts.
        Assert.Equal(
            [
                "And the promises it does not.",
                "The promise is kept in the statistics, not in the index.",
                "The scanner first.",
                "The writes come in the order of the locks.",
            ],
            gathered.Split(" | ").OrderBy(part => part, StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void Two_gatherings_in_one_query_take_their_own_order_and_filter()
    {
        using var database = ForumDatabase.Filled(nameof(Two_gatherings_in_one_query_take_their_own_order_and_filter));
        using var context = database.Context();

        var rows = context.Posts
            .GroupBy(p => p.AuthorId)
            .Select(g => new
            {
                AuthorId = g.Key,
                LongestFirst = string.Join(", ", g.OrderByDescending(p => p.Body.Length).Select(p => p.Body)),
                ShortestVisibleFirst = string.Join(", ", g.Where(p => !p.IsHidden).OrderBy(p => p.Body.Length).Select(p => p.Body)),
            })
            .OrderBy(r => r.AuthorId)
            .ToList();

        Assert.Equal(4, rows.Count);

        // The first author wrote four answers, and none of them is hidden.
        Assert.Equal(
            "The promise is kept in the statistics, not in the index., The writes come in the order of the locks., "
            + "And the promises it does not., The scanner first.",
            rows[0].LongestFirst);

        Assert.Equal(
            "The scanner first., And the promises it does not., The writes come in the order of the locks., "
            + "The promise is kept in the statistics, not in the index.",
            rows[0].ShortestVisibleFirst);

        // The third author wrote four answers, and one of them is hidden.
        Assert.Equal(
            "The order of the nodes is the order of the work., The tree is walked and the code is written., "
            + "The abstraction is a cost., The old address is dead.",
            rows[2].LongestFirst);

        Assert.Equal(
            "The abstraction is a cost., The tree is walked and the code is written., "
            + "The order of the nodes is the order of the work.",
            rows[2].ShortestVisibleFirst);
    }

    [Fact]
    public void The_same_answers_are_gathered_once()
    {
        using var database = ForumDatabase.Filled(nameof(The_same_answers_are_gathered_once));
        using var context = database.Context();

        // The notes of the moderators: five of the seven topics have no note, and the two that
        // have one are different.
        string gathered = string.Join(", ", context.Topics.Select(t => t.ModeratorNote!).Distinct());

        Assert.Equal(3, gathered.Split(", ").Length);
    }
}
