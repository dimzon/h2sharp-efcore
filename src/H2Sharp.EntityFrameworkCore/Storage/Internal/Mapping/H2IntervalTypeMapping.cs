using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2IntervalTypeMapping : H2TypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static H2IntervalTypeMapping Default { get; } = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2IntervalTypeMapping()
        : this(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(typeof(TimeSpan), jsonValueReaderWriter: H2JsonTimeSpanReaderWriter.Instance),
                "interval day to second"))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2IntervalTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2IntervalTypeMapping(parameters);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string ProcessStoreType(RelationalTypeMappingParameters parameters, string storeType, string _)
        // H2 spells the interval types with their qualifier -- INTERVAL DAY TO SECOND and the like -- and
        // a bare INTERVAL is not a column type. The engine puts a TimeSpan into DAY TO SECOND, so that is
        // the qualifier the column has to carry.
        => parameters.Precision is null ? storeType : $"{storeType}({parameters.Precision})";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    /// <remarks>
    ///     The value is written as a cast of its text and not as an interval literal. H2 2.5 knows
    ///     the literal "INTERVAL '1 2:3:4' DAY TO SECOND" and refuses the bare "INTERVAL '1 2:3:4'"
    ///     (the reference answers "Cannot parse "INTERVAL" constant "1 2:3:4""), and it knows the
    ///     cast of the same text to the same type, which is the form the engine of this port
    ///     reads. Both are H2's own forms of one thing, and the value they give is the same:
    ///     the reference answers "INTERVAL '1 02:03:04' DAY TO SECOND" for either.
    /// </remarks>
    protected override string GenerateNonNullSqlLiteral(object value)
        => Dialect.AppendInterval(new StringBuilder(), (TimeSpan)value).ToString();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateEmbeddedNonNullSqlLiteral(object value)
        => $"""
            "{FormatTimeSpanAsInterval((TimeSpan)value)}"
            """;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static string FormatTimeSpanAsInterval(TimeSpan ts)
    {
        // H2 reads an interval text with the day count spelled out: "0 10:09:08.007" is
        // accepted and "10:09:08.007" is refused (checked on the reference). So the day count
        // is always written, zero included, and the sign travels with it.
        var sign = ts < TimeSpan.Zero ? "-" : "";
        var ticks = ts.Duration();
        var time = ticks.Ticks % 10000000 == 0
            ? ticks.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
            : ticks.ToString(@"hh\:mm\:ss\.FFFFFF", CultureInfo.InvariantCulture);
        return $"{sign}{ticks.Days} {time}";
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static TimeSpan ParseIntervalAsTimeSpan(ReadOnlySpan<char> s)
    {
        // We store the a PG-compatible interval representation in JSON so it can be queried out, but unfortunately this isn't
        // compatible with TimeSpan, so we have to parse manually.

        // Note
        var isNegated = false;
        if (s[0] == '-')
        {
            isNegated = true;
            s = s[1..];
        }

        int i;
        for (i = 0; i < s.Length; i++)
        {
            if (!char.IsDigit(s[i]))
            {
                break;
            }
        }

        if (i == s.Length)
        {
            throw new FormatException();
        }

        // Space is the separator between the days and the hours:minutes:seconds components
        var days = 0;
        if (s[i] == ' ')
        {
            days = int.Parse(s[..i]);
            s = s[i..];
        }

        var timeSpan = TimeSpan.Parse(s, CultureInfo.InvariantCulture);

        // We've already extracted the days, so there shouldn't be a days component in the rest (indicates an incompatible/malformed string)
        if (timeSpan.Days != 0)
        {
            throw new FormatException();
        }

        if (days > 0)
        {
            timeSpan = new TimeSpan(
                days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds, timeSpan.Microseconds);
        }

        if (isNegated)
        {
            timeSpan = -timeSpan;
        }

        return timeSpan;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public sealed class H2JsonTimeSpanReaderWriter : JsonValueReaderWriter<TimeSpan>
    {
        private static readonly PropertyInfo InstanceProperty = typeof(H2JsonTimeSpanReaderWriter).GetProperty(nameof(Instance))!;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static H2JsonTimeSpanReaderWriter Instance { get; } = new();

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override TimeSpan FromJsonTyped(ref Utf8JsonReaderManager manager, object? existingObject = null)
            => ParseIntervalAsTimeSpan(manager.CurrentReader.GetString()!);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override void ToJsonTyped(Utf8JsonWriter writer, TimeSpan value)
            => writer.WriteStringValue(FormatTimeSpanAsInterval(value));

        /// <inheritdoc />
        public override Expression ConstructorExpression => Expression.Property(null, InstanceProperty);
    }
}
