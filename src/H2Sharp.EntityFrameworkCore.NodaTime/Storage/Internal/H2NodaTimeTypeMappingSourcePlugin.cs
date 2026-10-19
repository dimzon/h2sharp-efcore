using System.Collections.Concurrent;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2NodaTimeTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
#if DEBUG
    internal static bool LegacyTimestampBehavior;
    internal static bool DisableDateTimeInfinityConversions;
#else
    internal static readonly bool LegacyTimestampBehavior;
    internal static readonly bool DisableDateTimeInfinityConversions;
#endif

    static H2NodaTimeTypeMappingSourcePlugin()
    {
        LegacyTimestampBehavior = AppContext.TryGetSwitch("H2.EnableLegacyTimestampBehavior", out var enabled) && enabled;
        DisableDateTimeInfinityConversions = AppContext.TryGetSwitch("H2.DisableDateTimeInfinityConversions", out enabled) && enabled;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ConcurrentDictionary<string, RelationalTypeMapping[]> StoreTypeMappings { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ConcurrentDictionary<Type, RelationalTypeMapping> ClrTypeMappings { get; }

    #region TypeMapping

    private readonly TimestampLocalDateTimeMapping _timestampLocalDateTime = TimestampLocalDateTimeMapping.Default;
    private readonly LegacyTimestampInstantMapping _legacyTimestampInstant = LegacyTimestampInstantMapping.Default;

    private readonly TimestampTzInstantMapping _timestamptzInstant = TimestampTzInstantMapping.Default;
    private readonly TimestampTzZonedDateTimeMapping _timestamptzZonedDateTime = TimestampTzZonedDateTimeMapping.Default;
    private readonly TimestampTzOffsetDateTimeMapping _timestamptzOffsetDateTime = TimestampTzOffsetDateTimeMapping.Default;

    private readonly DateMapping _date = DateMapping.Default;
    private readonly TimeMapping _time = TimeMapping.Default;
    private readonly TimeTzMapping _timetz = TimeTzMapping.Default;
    private readonly PeriodIntervalMapping _periodInterval = PeriodIntervalMapping.Default;
    private readonly DurationIntervalMapping _durationInterval = DurationIntervalMapping.Default;

    // PostgreSQL has no native type for representing time zones - it just uses the IANA ID as text.
    private readonly DateTimeZoneMapping _timeZone = new("text");

    // Built-in ranges
    private readonly H2RangeTypeMapping _timestampLocalDateTimeRange;
    private readonly H2RangeTypeMapping _legacyTimestampInstantRange;
    private readonly H2RangeTypeMapping _timestamptzInstantRange;
    private readonly H2RangeTypeMapping _timestamptzZonedDateTimeRange;
    private readonly H2RangeTypeMapping _timestamptzOffsetDateTimeRange;
    private readonly H2RangeTypeMapping _dateRange;
    private readonly DateIntervalRangeMapping _dateIntervalRange = new();
    private readonly IntervalRangeMapping _intervalRange = new();

    #endregion

    /// <summary>
    ///     Constructs an instance of the <see cref="H2NodaTimeTypeMappingSourcePlugin" /> class.
    /// </summary>
    public H2NodaTimeTypeMappingSourcePlugin(ISqlGenerationHelper sqlGenerationHelper)
    {
        _timestampLocalDateTimeRange = H2RangeTypeMapping.CreatBuiltInRangeMapping(
            "tsrange", typeof(H2Range<LocalDateTime>), H2DbType.TimestampRange, _timestampLocalDateTime);
        _legacyTimestampInstantRange = H2RangeTypeMapping.CreatBuiltInRangeMapping(
            "tsrange", typeof(H2Range<Instant>), H2DbType.TimestampRange, _legacyTimestampInstant);
        _timestamptzInstantRange = H2RangeTypeMapping.CreatBuiltInRangeMapping(
            "tstzrange", typeof(H2Range<Instant>), H2DbType.TimestampTzRange, _timestamptzInstant);
        _timestamptzZonedDateTimeRange = H2RangeTypeMapping.CreatBuiltInRangeMapping(
            "tstzrange", typeof(H2Range<ZonedDateTime>), H2DbType.TimestampTzRange, _timestamptzZonedDateTime);
        _timestamptzOffsetDateTimeRange = H2RangeTypeMapping.CreatBuiltInRangeMapping(
            "tstzrange", typeof(H2Range<OffsetDateTime>), H2DbType.TimestampTzRange, _timestamptzOffsetDateTime);
        _dateRange = H2RangeTypeMapping.CreatBuiltInRangeMapping(
            "daterange", typeof(H2Range<LocalDate>), H2DbType.DateRange, _date);

        var storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
        {
            {
                // We currently allow _legacyTimestampInstant even in non-legacy mode, since when upgrading to 6.0 with existing
                // migrations, model snapshots still contain old mappings (Instant mapped to timestamp), and EF Core's model differ
                // expects type mappings to be found for these. See https://github.com/dotnet/efcore/issues/26168.
                "timestamp without time zone", LegacyTimestampBehavior
                    ? [_legacyTimestampInstant, _timestampLocalDateTime]
                    : [_timestampLocalDateTime, _legacyTimestampInstant]
            },
            {
                "timestamp with time zone", [_timestamptzInstant, _timestamptzZonedDateTime, _timestamptzOffsetDateTime]
            },
            { "date", [_date] },
            { "time without time zone", [_time] },
            { "time with time zone", [_timetz] },
            { "interval", [_periodInterval, _durationInterval] },
            {
                "tsrange", LegacyTimestampBehavior
                    ? [_legacyTimestampInstantRange, _timestampLocalDateTimeRange]
                    : [_timestampLocalDateTimeRange, _legacyTimestampInstantRange]
            },
            {
                "tstzrange", [
                    _intervalRange, _timestamptzInstantRange, _timestamptzZonedDateTimeRange, _timestamptzOffsetDateTimeRange
                ]
            },
            { "daterange", [_dateIntervalRange, _dateRange] }
        };

        // Set up aliases
        storeTypeMappings["timestamp"] = storeTypeMappings["timestamp without time zone"];
        storeTypeMappings["timestamptz"] = storeTypeMappings["timestamp with time zone"];
        storeTypeMappings["time"] = storeTypeMappings["time without time zone"];
        storeTypeMappings["timetz"] = storeTypeMappings["time with time zone"];

        var clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
        {
            { typeof(Instant), LegacyTimestampBehavior ? _legacyTimestampInstant : _timestamptzInstant },
            { typeof(LocalDateTime), _timestampLocalDateTime },
            { typeof(ZonedDateTime), _timestamptzZonedDateTime },
            { typeof(OffsetDateTime), _timestamptzOffsetDateTime },
            { typeof(LocalDate), _date },
            { typeof(LocalTime), _time },
            { typeof(OffsetTime), _timetz },
            { typeof(Period), _periodInterval },
            { typeof(Duration), _durationInterval },
            // See DateTimeZone below

            { typeof(H2Range<Instant>), LegacyTimestampBehavior ? _legacyTimestampInstantRange : _timestamptzInstantRange },
            { typeof(H2Range<LocalDateTime>), _timestampLocalDateTimeRange },
            { typeof(H2Range<ZonedDateTime>), _timestamptzZonedDateTimeRange },
            { typeof(H2Range<OffsetDateTime>), _timestamptzOffsetDateTimeRange },
            { typeof(H2Range<LocalDate>), _dateRange },
            { typeof(DateInterval), _dateIntervalRange },
            { typeof(Interval), _intervalRange },
        };

        StoreTypeMappings = new ConcurrentDictionary<string, RelationalTypeMapping[]>(storeTypeMappings, StringComparer.OrdinalIgnoreCase);
        ClrTypeMappings = new ConcurrentDictionary<Type, RelationalTypeMapping>(clrTypeMappings);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
        => FindBaseMapping(mappingInfo)?.Clone(mappingInfo);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual RelationalTypeMapping? FindBaseMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;
        var storeTypeNameBase = mappingInfo.StoreTypeNameBase;

        if (storeTypeName is not null)
        {
            if (StoreTypeMappings.TryGetValue(storeTypeName, out var mappings))
            {
                if (clrType is null)
                {
                    return mappings[0];
                }

                foreach (var m in mappings)
                {
                    if (m.ClrType == clrType)
                    {
                        return m;
                    }
                }

                return null;
            }

            if (StoreTypeMappings.TryGetValue(storeTypeNameBase!, out mappings))
            {
                if (clrType is null)
                {
                    return mappings[0];
                }

                foreach (var m in mappings)
                {
                    if (m.ClrType == clrType)
                    {
                        return m;
                    }
                }

                return null;
            }
        }

        if (clrType is not null)
        {
            if (ClrTypeMappings.TryGetValue(clrType, out var mapping))
            {
                return mapping;
            }

            if (clrType.IsAssignableTo(typeof(DateTimeZone)))
            {
                return _timeZone;
            }
        }

        return null;
    }
}
