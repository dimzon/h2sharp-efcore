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

    private static void AssertClose(double expected, string? actual)
        => Assert.Equal(expected, double.Parse(actual!, CultureInfo.InvariantCulture), precision: 7);
}
