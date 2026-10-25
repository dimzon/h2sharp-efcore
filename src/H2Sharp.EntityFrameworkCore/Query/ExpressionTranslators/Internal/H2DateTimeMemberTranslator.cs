using System.Diagnostics.CodeAnalysis;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     Provides translation services for <see cref="DateTime" /> members.
/// </summary>
/// <remarks>
///     See: https://www.h2database.com/html/functions.html
/// </remarks>
public class H2DateTimeMemberTranslator : IMemberTranslator
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly RelationalTypeMapping _timestampMapping;
    private readonly RelationalTypeMapping _timestampTzMapping;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2DateTimeMemberTranslator(IRelationalTypeMappingSource typeMappingSource, H2SqlExpressionFactory sqlExpressionFactory)
    {
        _typeMappingSource = typeMappingSource;
        _timestampMapping = typeMappingSource.FindMapping(typeof(DateTime), "timestamp without time zone")!;
        _timestampTzMapping = typeMappingSource.FindMapping(typeof(DateTime), "timestamp with time zone")!;
        _sqlExpressionFactory = sqlExpressionFactory;
    }

    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        var declaringType = member.DeclaringType;

        if (declaringType != typeof(DateTime)
            && declaringType != typeof(DateTimeOffset)
            && declaringType != typeof(DateOnly)
            && declaringType != typeof(TimeOnly))
        {
            return null;
        }

        if (declaringType == typeof(DateTimeOffset)
            && instance is not null
            && TranslateDateTimeOffset(instance, member) is { } translated)
        {
            return translated;
        }

        if (declaringType == typeof(DateOnly) && TranslateDateOnly(instance, member) is { } translated2)
        {
            return translated2;
        }

        if (member.Name == nameof(DateTime.Date))
        {
            // Note that DateTime.Date returns a DateTime, not a DateOnly (introduced later); so we truncate rather than convert to a
            // date (compare with NodaTime where we want a LocalDate).

            // The parts of a timestamp with a zone are read in the value's own offset, and the
            // wall clock of a DateTime is exactly that. H2 reads neither the three-argument
            // DATE_TRUNC nor a CAST of the value to a timestamp that way: both use the zone of
            // the session. So the wall clock is written out and read back.
            switch (instance)
            {
                case { TypeMapping: H2TimestampTypeMapping }:
                case { } when H2TypeMappingSource.LegacyTimestampBehavior:
                    return _sqlExpressionFactory.Function(
                        "date_trunc",
                        [_sqlExpressionFactory.Constant("day"), instance],
                        nullable: true,
                        argumentsPropagateNullability: TrueArrays[2],
                        returnType,
                        instance.TypeMapping);

                case { TypeMapping: H2TimestampTzTypeMapping }:
                    return WallClock(instance, "yyyy-MM-dd");

                // If DateTime.Date is invoked on a PostgreSQL date (or DateOnly, which can only be mapped to datE), simply no-op.
                case { TypeMapping: H2DateTimeDateTypeMapping }:
                case { Type: var type } when type == typeof(DateOnly):
                    return instance;

                default:
                    return null;
            }
        }

        return member.Name switch
        {
            // Legacy behavior
            nameof(DateTime.Now) when H2TypeMappingSource.LegacyTimestampBehavior
                => UtcNow(),
            nameof(DateTime.UtcNow) when H2TypeMappingSource.LegacyTimestampBehavior
                => _sqlExpressionFactory.AtUtc(UtcNow()), // Return a UTC timestamp, but as timestamp without time zone

            // We support getting a local DateTime via DateTime.Now (based on PG TimeZone), but there's no way to get a non-UTC
            // DateTimeOffset.
            nameof(DateTime.Now) => declaringType == typeof(DateTimeOffset)
                ? throw new InvalidOperationException("Cannot translate DateTimeOffset.Now - use UtcNow.")
                : LocalNow(),
            nameof(DateTime.UtcNow) => UtcNow(),

            nameof(DateTime.Today) => _sqlExpressionFactory.Function(
                "date_trunc",
                [_sqlExpressionFactory.Constant("day"), LocalNow()],
                nullable: false,
                argumentsPropagateNullability: FalseArrays[2],
                typeof(DateTime),
                _timestampMapping),

            nameof(DateTime.Year) => DatePart(instance!, "year"),
            nameof(DateTime.Month) => DatePart(instance!, "month"),
            nameof(DateTime.DayOfYear) => DatePart(instance!, "doy"),
            nameof(DateTime.Day) => DatePart(instance!, "day"),
            nameof(DateTime.Hour) => DatePart(instance!, "hour"),
            nameof(DateTime.Minute) => DatePart(instance!, "minute"),
            nameof(DateTime.Second) => DatePart(instance!, "second"),

            nameof(DateTime.Millisecond) => null, // Too annoying

            // .NET's DayOfWeek counts the week from Sunday, and the ISO numbering counts it
            // from Monday, so the number of .NET is the ISO one by seven.
            nameof(DateTime.DayOfWeek) => DatePart(instance!, "dow"),

            // Casting a timestamptz to time (to get the time component) converts it to a local timestamp based on TimeZone.
            // Convert to a timestamp without time zone at UTC to get the right values.
            nameof(DateTime.TimeOfDay) when TryConvertAwayFromTimestampTz(instance!, out var convertedInstance)
                => _sqlExpressionFactory.Convert(
                    convertedInstance,
                    typeof(TimeSpan),
                    _typeMappingSource.FindMapping(typeof(TimeSpan), storeTypeName: "time")),

            // TODO: Should be possible
            nameof(DateTime.Ticks) => null,

            _ => null
        };

        SqlExpression UtcNow()
            // CURRENT_TIMESTAMP and not NOW(): in H2 2.5 NOW() is another name of
            // LOCALTIMESTAMP, which is a timestamp without a time zone, while
            // CURRENT_TIMESTAMP is the one with a time zone. The reference answers
            // "2026-10-20 22:53:05.427709+03" for CURRENT_TIMESTAMP and
            // "2026-10-20 22:53:05.427709" for NOW().
            => _sqlExpressionFactory.Function(
                "current_timestamp",
                [],
                nullable: false,
                argumentsPropagateNullability: TrueArrays[0],
                returnType,
                _timestampTzMapping);

        SqlExpression LocalNow()
            => _sqlExpressionFactory.Convert(UtcNow(), returnType, _timestampMapping);
    }

    private SqlExpression? DatePart(
        SqlExpression instance,
        string partName)
    {
        // H2 has no date_part. The part of a date is taken by a function of its own --
        // YEAR, MONTH, DAY, DAY_OF_YEAR, HOUR, MINUTE, SECOND -- and the reference refuses
        // date_part with "Function "DATE_PART" not found". The day of the week is
        // ISO_DAY_OF_WEEK, and its number is taken by seven, because .NET counts the week
        // from Sunday and ISO counts it from Monday.
        //
        // H2 reads the parts of a timestamp with a zone in the value's own offset, which is
        // what .NET wants; a conversion to a timestamp first is not needed, and a CAST would
        // read the value in the zone of the session instead.
        if (partName == "dow")
        {
            var dayOfWeek = _sqlExpressionFactory.Function(
                "iso_day_of_week",
                [instance],
                nullable: true,
                argumentsPropagateNullability: TrueArrays[1],
                typeof(int));

            return _sqlExpressionFactory.Function(
                "mod",
                [dayOfWeek, _sqlExpressionFactory.Constant(7)],
                nullable: true,
                argumentsPropagateNullability: TrueArrays[2],
                typeof(int));
        }

        var functionName = partName switch
        {
            "year" => "year",
            "month" => "month",
            "doy" => "day_of_year",
            "day" => "day",
            "hour" => "hour",
            "minute" => "minute",
            "second" => "second",
            _ => throw new ArgumentOutOfRangeException(nameof(partName), partName, "Unknown date part")
        };

        return _sqlExpressionFactory.Function(
            functionName,
            [instance],
            nullable: true,
            argumentsPropagateNullability: TrueArrays[1],
            typeof(int));
    }

    private SqlExpression? TranslateDateTimeOffset(SqlExpression instance, MemberInfo member)
        => member.Name switch
        {
            // The wall clock of the value itself: H2 keeps the offset the value was written
            // with, and FORMATDATETIME without a named zone writes that offset's wall clock.
            // A CAST of the value to a timestamp would read it in the zone of the session.
            nameof(DateTimeOffset.DateTime) => WallClock(instance, "yyyy-MM-dd HH:mm:ss.SSSSSSSSS"),

            // The instant in UTC: FORMATDATETIME with a named zone writes the wall clock of
            // that zone. A cast of the value to a timestamp would read it in the session zone
            // even after AT TIME ZONE restated the value in UTC.
            nameof(DateTimeOffset.UtcDateTime) => WallClock(instance, "yyyy-MM-dd HH:mm:ss.SSSSSSSSS", "UTC"),

            // A CAST of a timestamp with a zone to a timestamp without one reads the value in
            // the zone of the session, which is the local time of .NET.
            nameof(DateTimeOffset.LocalDateTime) => _sqlExpressionFactory.Convert(instance, typeof(DateTime), _timestampMapping),

            // The date of the value's own offset, at midnight.
            nameof(DateTimeOffset.Date) => WallClock(instance, "yyyy-MM-dd"),

            _ => null
        };

    // The wall clock of a timestamp with a zone, written out by FORMATDATETIME in the zone of
    // the value (or the named zone) and read back as a timestamp without a zone.
    private SqlExpression WallClock(SqlExpression timestampWithZone, string pattern, string? zone = null)
    {
        SqlExpression[] arguments = zone is null
            ?
            [
                timestampWithZone, _sqlExpressionFactory.Constant(pattern),
                _sqlExpressionFactory.Constant("en")
            ]
            :
            [
                timestampWithZone, _sqlExpressionFactory.Constant(pattern),
                _sqlExpressionFactory.Constant("en"), _sqlExpressionFactory.Constant(zone)
            ];

        return _sqlExpressionFactory.Convert(
            _sqlExpressionFactory.Function(
                "FORMATDATETIME",
                arguments,
                nullable: true,
                argumentsPropagateNullability: arguments.Select(_ => true).ToArray(),
                typeof(string)),
            typeof(DateTime),
            _timestampMapping);
    }

    private SqlExpression? TranslateDateOnly(SqlExpression? instance, MemberInfo member)
        => member.Name switch
        {
            // We use fragment rather than a DateOnly constant, since 0001-01-01 gets rendered as -infinity by default.
            // TODO: Set the right type/type mapping after https://github.com/dotnet/efcore/pull/34995 is merged
            nameof(DateOnly.DayNumber) when instance is not null
                => _sqlExpressionFactory.Function(
                    "DATEDIFF",
                    [_sqlExpressionFactory.Constant("DAY"), _sqlExpressionFactory.Fragment("DATE '0001-01-01'"), instance],
                    nullable: true,
                    argumentsPropagateNullability: [false, false, true],
                    typeof(int)),

            _ => null
        };

    // Various conversion functions translated here (date_part, ::time) exist only for timestamp without time zone, so if we pass in a
    // timestamptz it gets implicitly converted to a local timestamp based on TimeZone; that's the wrong behavior (these conversions are not
    // supposed to be sensitive to TimeZone).
    // To avoid this, if we get a timestamptz, convert it to a timestamp without time zone (at UTC), which doesn't undergo any timezone
    // conversions.
    private bool TryConvertAwayFromTimestampTz(SqlExpression timestamp, [NotNullWhen(true)] out SqlExpression? result)
    {
        switch (timestamp)
        {
            // We're already dealing with a non-timestamptz mapping, no conversion needed.
            case { TypeMapping: H2TimestampTypeMapping or H2DateTimeDateTypeMapping or H2TimeTypeMapping }:
            case { Type: var type } when type == typeof(DateOnly) || type == typeof(TimeOnly):
                result = timestamp;
                return true;

            // In these cases we know that the expression represents a timestamptz; it's safe to convert to a timestamp without time zone.
            // Note that timestamptz AT TIME ZONE 'UTC' returns the same timestamp but as a timestamp (without time zone).
            case { TypeMapping: H2TimestampTzTypeMapping }:
            case { Type: var type } when type == typeof(DateTimeOffset):
                result = _sqlExpressionFactory.AtUtc(timestamp);
                return true;

            // If it's a DateTime who's type mapping isn't known (parameter), we cannot ensure that a timestamp without time zone
            // is returned (note that applying AT TIME ZONE 'UTC' on a timestamp without time zone would yield a timestamptz, which would
            // again undergo timestamp conversion)
            case { Type: var type } when type == typeof(DateTime):
                result = null;
                return false;

            default:
                throw new UnreachableException();
        }
    }
}
