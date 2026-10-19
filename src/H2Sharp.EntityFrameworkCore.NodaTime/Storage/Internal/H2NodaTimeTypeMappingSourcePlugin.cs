using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     Maps the NodaTime types onto the H2 column types that hold them.
/// </summary>
/// <remarks>
///     <para>
///         Upstream these mappings left the value conversion to the ADO driver, through the type
///         handlers in the Npgsql.NodaTime package. H2Sharp's ADO layer knows only the BCL types, so
///         each mapping here carries the conversion itself: the mapping's CLR type is the NodaTime type
///         the model holds, and its converter turns the value into the BCL type the engine
///         understands before it reaches the parameter.
///     </para>
///     <para>
///         The store types are H2's own: DATE for a local date, TIME for a local time, TIMESTAMP for a
///         local date and time, TIMESTAMP WITH TIME ZONE for an instant or an offset date and time,
///         INTERVAL for a duration.
///     </para>
///     <para>
///         Two NodaTime types are deliberately absent, because H2 cannot represent them and a lossy
///         conversion would answer wrongly rather than refuse: <see cref="Period" /> counts months and
///         years, which no BCL type holds, and <see cref="OffsetTime" /> is a time of day with an offset,
///         which H2 has no column type for. A property of either type gets no mapping, and EF Core says
///         so instead of quietly storing something else.
///     </para>
/// </remarks>
public class H2NodaTimeTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;

        if (clrType is null)
        {
            return null;
        }

        var storeType = mappingInfo.StoreTypeName;

        if (clrType == typeof(LocalDate))
        {
            return storeType is null
                ? new H2NodaTimeTypeMapping("date", clrType, NodaTimeValueConverters.LocalDateConverter)
                : null;
        }

        if (clrType == typeof(LocalTime))
        {
            return storeType is null
                ? new H2NodaTimeTypeMapping("time", clrType, NodaTimeValueConverters.LocalTimeConverter)
                : null;
        }

        if (clrType == typeof(LocalDateTime))
        {
            return storeType is null
                ? new H2NodaTimeTypeMapping("timestamp", clrType, NodaTimeValueConverters.LocalDateTimeConverter)
                : null;
        }

        if (clrType == typeof(Instant) || clrType == typeof(OffsetDateTime) || clrType == typeof(ZonedDateTime))
        {
            if (storeType is not null)
            {
                return null;
            }

            ValueConverter? converter = clrType == typeof(Instant)
                ? NodaTimeValueConverters.InstantConverter
                : clrType == typeof(OffsetDateTime)
                    ? NodaTimeValueConverters.OffsetDateTimeConverter
                    : NodaTimeValueConverters.ZonedDateTimeConverter;

            return new H2NodaTimeTypeMapping("timestamp with time zone", clrType, converter);
        }

        if (clrType == typeof(Duration))
        {
            return storeType is null
                ? new H2NodaTimeTypeMapping("interval day to second", clrType, NodaTimeValueConverters.DurationConverter)
                : null;
        }

        if (clrType == typeof(DateTimeZone))
        {
            return storeType is null
                ? new H2NodaTimeTypeMapping("clob", clrType, NodaTimeValueConverters.DateTimeZoneConverter)
                : null;
        }

        return null;
    }
}

/// <summary>
///     A mapping for an NodaTime type, carrying the conversion to the type the engine understands.
/// </summary>
public class H2NodaTimeTypeMapping : H2TypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2NodaTimeTypeMapping(string storeType, Type clrType, ValueConverter? converter)
        : base(storeType, clrType, converter)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2NodaTimeTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2NodaTimeTypeMapping(parameters);
}
