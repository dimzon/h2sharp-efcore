using System.Globalization;
using System.Text.Json;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Dashboard reports compute distribution and regression from the forum's actual topic rows.
/// </summary>
public class ForumStatisticsExamples
{
    [Fact]
    public void Dashboard_measures_the_distribution_of_views()
    {
        using var database = ForumDatabase.Filled(nameof(Dashboard_measures_the_distribution_of_views));
        using var context = database.Context();
        var query = context.Topics.GroupBy(t => t.ForumId).OrderBy(g => g.Key).Select(g => new
        {
            Forum = g.Key,
            Sample = EF.Functions.VarianceSample(g.Select(t => t.ViewCount)),
            Population = EF.Functions.VariancePopulation(g.Select(t => t.ViewCount)),
            SampleDeviation = EF.Functions.StandardDeviationSample(g.Select(t => t.ViewCount)),
            PopulationDeviation = EF.Functions.StandardDeviationPopulation(g.Select(t => t.ViewCount))
        });
        var rows = ReferenceForum.Query(context, query);
        var groups = ForumData.Topics().GroupBy(t => t.ForumId).OrderBy(g => g.Key).ToArray();
        for (var i = 0; i < groups.Length; i++)
        {
            var values = groups[i].Select(t => (double)t.ViewCount).ToArray();
            var mean = values.Average();
            var sum = values.Sum(value => Math.Pow(value - mean, 2));
            AssertClose(sum / values.Length, rows[i][2]);
            AssertClose(Math.Sqrt(sum / values.Length), rows[i][4]);
            if (values.Length > 1)
            {
                AssertClose(sum / (values.Length - 1), rows[i][1]);
                AssertClose(Math.Sqrt(sum / (values.Length - 1)), rows[i][3]);
            }
            else
            {
                Assert.Null(rows[i][1]);
                Assert.Null(rows[i][3]);
            }
        }
    }

    [Fact]
    public void Dashboard_checks_the_relationship_between_topic_age_and_views()
    {
        using var database = ForumDatabase.Filled(nameof(Dashboard_checks_the_relationship_between_topic_age_and_views));
        using var context = database.Context();
        var query = context.Topics.Where(t => t.ForumId == 1).GroupBy(t => t.ForumId).Select(g => new
        {
            Correlation = EF.Functions.Correlation(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            Covariance = EF.Functions.CovariancePopulation(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            SampleCovariance = EF.Functions.CovarianceSample(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            Count = EF.Functions.RegrCount(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            AverageX = EF.Functions.RegrAverageX(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            AverageY = EF.Functions.RegrAverageY(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            Slope = EF.Functions.RegrSlope(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            Intercept = EF.Functions.RegrIntercept(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            R2 = EF.Functions.RegrR2(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            Sxx = EF.Functions.RegrSXX(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId))),
            Sxy = EF.Functions.RegrSXY(g.Select(t => new ValueTuple<double, double>(t.ViewCount, t.AuthorId)))
        });
        var row = Assert.Single(ReferenceForum.Query(context, query));
        var values = ForumData.Topics().Where(t => t.ForumId == 1).ToArray();
        var x = values.Average(t => t.AuthorId);
        var y = values.Average(t => t.ViewCount);
        var xx = values.Sum(t => Math.Pow(t.AuthorId - x, 2));
        var yy = values.Sum(t => Math.Pow(t.ViewCount - y, 2));
        var xy = values.Sum(t => (t.AuthorId - x) * (t.ViewCount - y));
        AssertClose(xy / Math.Sqrt(xx * yy), row[0]);
        AssertClose(xy / values.Length, row[1]);
        AssertClose(xy / (values.Length - 1), row[2]);
        Assert.Equal("3", row[3]);
        AssertClose(x, row[4]); AssertClose(y, row[5]);
        AssertClose(xy / xx, row[6]); AssertClose(y - x * xy / xx, row[7]);
        AssertClose(xy * xy / (xx * yy), row[8]);
        AssertClose(xx, row[9]); AssertClose(xy, row[10]);
    }

    [Fact]
    public void Dashboard_exports_topics_as_ordered_arrays_and_json()
    {
        using var database = ForumDatabase.Filled(nameof(Dashboard_exports_topics_as_ordered_arrays_and_json));
        using var context = database.Context();
        var query = context.Topics.GroupBy(t => t.ForumId).OrderBy(g => g.Key).Select(g => new
        {
            Forum = g.Key,
            Names = EF.Functions.ArrayAgg(g.OrderBy(t => t.ViewCount).Select(t => t.Title)),
            Json = EF.Functions.JsonAgg(g.OrderBy(t => t.ViewCount).Select(t => t.Title)),
            Object = EF.Functions.JsonObjectAgg(g.Select(t => new ValueTuple<string, int>(t.Title, t.ViewCount))),
            Caption = string.Concat(g.OrderBy(t => t.ViewCount).Select(t => t.Title))
        });
        var rows = ReferenceForum.Query(context, query);
        var groups = ForumData.Topics().GroupBy(t => t.ForumId).OrderBy(g => g.Key).ToArray();
        for (var i = 0; i < groups.Length; i++)
        {
            var names = groups[i].OrderBy(t => t.ViewCount).Select(t => t.Title).ToArray();
            Assert.Equal(names, JsonSerializer.Deserialize<string[]>(rows[i][2]!));
            var objects = JsonSerializer.Deserialize<Dictionary<string, int>>(rows[i][3]!)!;
            Assert.Equal(groups[i].Count(), objects.Count);
            Assert.All(groups[i], t => Assert.Equal(t.ViewCount, objects[t.Title]));
            Assert.Equal(string.Concat(names), rows[i][4]);
            Assert.Equal("[" + string.Join(", ", names) + "]", rows[i][1]);
        }
    }

    [Fact]
    public void Dashboard_reports_averages_totals_and_extremes()
    {
        using var database = ForumDatabase.Filled(nameof(Dashboard_reports_averages_totals_and_extremes));
        using var context = database.Context();

        Assert.Equal(3680.0 / 7, context.Topics.Average(t => t.ViewCount), precision: 7);
        Assert.Equal(14, context.Posts.LongCount());
        Assert.Equal(3680, context.Topics.Sum(t => (long)t.ViewCount));
        Assert.Equal(4.70m, context.Users.Average(u => u.Balance));
        Assert.Equal(1200, context.Topics.Max(t => t.ViewCount));
        Assert.Equal(80, context.Topics.Min(t => t.ViewCount));
        Assert.Equal(4, context.Posts.Select(p => p.AuthorId).Distinct().Count());
        Assert.Equal(3.00m, context.Users.Where(u => u.UserName == "grace").Select(u => EF.Functions.Truncate(u.Balance)).Single());
        Assert.Equal(
            [12, 3, 0, 7, 0],
            context.Users.OrderBy(u => u.Id).Select(u => EF.Functions.Truncate(u.Balance, 0)).ToList());
        Assert.Equal(
            [1, 0, -1, 1, -1, -1, -1],
            context.Topics.OrderBy(t => t.Id).Select(t => EF.Functions.Sign(t.ViewCount - 640)).ToList());
    }

    [Fact]
    public void Dashboard_exports_ordered_arrays_and_json_on_reference_h2()
    {
        using var database = ForumDatabase.Filled(nameof(Dashboard_exports_ordered_arrays_and_json_on_reference_h2));
        using var context = database.Context();

        // The engine does not read an ORDER BY inside ARRAY_AGG yet, so the reference
        // checks the whole statement: the names in view-count order, as JSON and as an array.
        var query = context.Topics.GroupBy(t => t.ForumId).OrderBy(g => g.Key).Select(g => new
        {
            Names = EF.Functions.ArrayAgg(g.OrderBy(t => t.ViewCount).Select(t => t.Title)),
            Json = EF.Functions.JsonAgg(g.OrderBy(t => t.ViewCount).Select(t => t.Title)),
            Object = EF.Functions.JsonObjectAgg(g.Select(t => new ValueTuple<string, int>(t.Title, t.ViewCount)))
        });
        var rows = ReferenceForum.Query(context, query);
        var groups = ForumData.Topics().GroupBy(t => t.ForumId).OrderBy(g => g.Key).ToArray();

        Assert.Equal(groups.Length, rows.Count);
        for (var i = 0; i < groups.Length; i++)
        {
            var ordered = groups[i].OrderBy(t => t.ViewCount).Select(t => t.Title).ToArray();

            // H2 renders an array column as [a, b, c]; no title of the forum holds a comma.
            Assert.Equal("[" + string.Join(", ", ordered) + "]", rows[i][0]);
            Assert.Equal(ordered, JsonSerializer.Deserialize<string[]>(rows[i][1]!));
            var objects = JsonSerializer.Deserialize<Dictionary<string, int>>(rows[i][2]!)!;
            Assert.Equal(groups[i].Count(), objects.Count);
            Assert.All(groups[i], t => Assert.Equal(t.ViewCount, objects[t.Title]));
        }
    }

    [Fact]
    public void Dashboard_joins_titles_without_an_order()
    {
        using var database = ForumDatabase.Filled(nameof(Dashboard_joins_titles_without_an_order));
        using var context = database.Context();

        // Without an order the text comes back in row order; the sets are what is checked.
        var joined = context.Topics.GroupBy(t => t.ForumId).OrderBy(g => g.Key)
            .Select(g => string.Join(", ", g.Select(t => t.Title)))
            .ToList();
        var groups = ForumData.Topics().GroupBy(t => t.ForumId).OrderBy(g => g.Key).ToArray();

        Assert.Equal(groups.Length, joined.Count);
        for (var i = 0; i < groups.Length; i++)
        {
            Assert.Equal(
                groups[i].Select(t => t.Title).OrderBy(t => t, StringComparer.Ordinal),
                joined[i].Split(", ").OrderBy(t => t, StringComparer.Ordinal).ToArray());
        }
    }

    [Fact]
    public void Balances_round_half_away_from_zero()
    {
        using var database = ForumDatabase.Filled(nameof(Balances_round_half_away_from_zero));
        using var context = database.Context();

        // H2 ROUND breaks ties away from zero, while .NET Math.Round breaks them to even.
        // The provider passes the value to H2, and the engine of this port answers the
        // same as the reference: 12.50 becomes 13 on both.
        var rounded = context.Users.OrderBy(u => u.Id).Select(u => Math.Round(u.Balance));
        Assert.Equal([13, 3, 0, 8, 0], rounded.ToList());
        Assert.Equal(
            ["13", "3", "0", "8", "0"],
            ReferenceForum.Query(context, rounded).Select(row => row[0]));
    }

    [Fact]
    public void Monthly_report_groups_topics_by_truncated_month()
    {
        using var database = ForumDatabase.Filled(nameof(Monthly_report_groups_topics_by_truncated_month));
        using var context = database.Context();

        var months = context.Topics
            .GroupBy(t => EF.Functions.DateTrunc("MONTH", t.CreatedAt))
            .OrderBy(g => g.Key)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToList();

        Assert.Equal(
            ForumData.Topics().GroupBy(t => new DateTime(t.CreatedAt.Year, t.CreatedAt.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Count()))
                .ToArray(),
            months.Select(m => (m.Month, m.Count)).ToArray());
    }

    private static void AssertClose(double expected, string? actual)
        => Assert.Equal(expected, double.Parse(actual!, CultureInfo.InvariantCulture), precision: 7);
}
