using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Runs the same application model, data and EF command on H2 2.5 while the port catches up.
/// </summary>
internal static class ReferenceForum
{
    public static IReadOnlyList<string?[]> Query(ForumContext context, IQueryable query)
    {
        using var command = query.CreateDbCommand();
        return H2ReferenceOracle.Query(Setup(context), Sql(command));
    }

    public static IReadOnlyList<string?[]> Query(IQueryable query, string setup)
    {
        using var command = query.CreateDbCommand();
        return H2ReferenceOracle.Query(setup, Sql(command));
    }

    public static string Sql(DbCommand command)
    {
        var values = command.Parameters.Cast<DbParameter>().ToDictionary(
            p => p.ParameterName.TrimStart('$'), p => Literal(p.Value));
        return Regex.Replace(command.CommandText, @"\$([A-Za-z_][A-Za-z_0-9]*)", match => values[match.Groups[1].Value]);
    }

    public static string Setup(ForumContext context)
    {
        var script = new StringBuilder(context.Database.GenerateCreateScript());
        Append("Forums", ForumData.Forums());
        Append("Users", ForumData.Users());
        Append("Topics", ForumData.Topics());
        Append("Posts", ForumData.Posts());
        Append("Tags", ForumData.Tags());
        Append("TopicTags", ForumData.TopicTags());
        Append("Votes", ForumData.Votes());
        return script.ToString();

        void Append<T>(string table, IEnumerable<T> rows)
        {
            var properties = typeof(T).GetProperties();
            var index = 0;
            foreach (var row in rows)
            {
                index++;
                script.Append("INSERT INTO \"").Append(table).Append("\" (")
                    .AppendJoin(", ", properties.Select(p => '"' + p.Name + '"'))
                    .Append(") VALUES (")
                    .AppendJoin(", ", properties.Select(p => Literal(p.Name == "Id" ? index : p.GetValue(row))))
                    .AppendLine(");");
            }
        }
    }

    public static string Literal(object? value) => value switch
    {
        null or DBNull => "NULL",
        string text => "'" + text.Replace("'", "''", StringComparison.Ordinal) + "'",
        char c => Literal(c.ToString()),
        bool boolean => boolean ? "TRUE" : "FALSE",
        DateTime date => "TIMESTAMP '" + date.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'",
        DateTimeOffset date => "TIMESTAMP WITH TIME ZONE '" + date.ToString("yyyy-MM-dd HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture) + "'",
        DateOnly date => "DATE '" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "'",
        TimeOnly time => "TIME '" + time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'",
        byte[] bytes => "X'" + Convert.ToHexString(bytes) + "'",
        TimeSpan span => span == TimeSpan.FromDays(span.TotalDays) && span.TotalDays == Math.Floor(span.TotalDays)
            ? $"INTERVAL '{span.TotalDays}' DAY"
            : $"INTERVAL '{Math.Floor(span.TotalDays)} {span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}' DAY TO SECOND",
        Enum role => Literal(role.ToString()),
        System.Collections.IEnumerable array => "ARRAY[" + string.Join(",", array.Cast<object?>().Select(Literal)) + "]",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => throw new NotSupportedException($"Reference parameter type: {value.GetType()}")
    };
}
