using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     Converts the NodaTime types this provider stores into the BCL types the H2 engine understands.
/// </summary>
/// <remarks>
///     <para>
///         Upstream these conversions were done by the ADO driver, through the type handlers in the
///         Npgsql.NodaTime package. H2Sharp's ADO layer knows only the BCL types -- its value mapper
///         turns a <see cref="DateTime" />, a <see cref="TimeSpan" /> and the rest into H2 values, and
///         has never heard of NodaTime. So the conversion happens here, in the provider, where EF Core
///         applies it to the value it hands to the parameter.
///     </para>
///     <para>
///         The type each one lands on is the type the main provider already maps to the right H2
///         column: DateOnly is a DATE, TimeOnly a TIME, DateTime a TIMESTAMP, DateTimeOffset a
///         TIMESTAMP WITH TIME ZONE, TimeSpan an INTERVAL.
///     </para>
///     <para>
///         Two NodaTime types are deliberately absent, because H2 cannot represent them and a lossy
///         conversion would answer wrongly rather than refuse: <see cref="Period" /> counts months and
///         years, which no BCL type holds, and <see cref="OffsetTime" /> is a time of day with an offset,
///         which H2 has no column type for. A property of either type gets no mapping, and EF Core says
///         so instead of quietly storing something else.
///     </para>
/// </remarks>
public static class NodaTimeValueConverters
{
    /// <summary>
    ///     A local date, stored as an H2 DATE.
    /// </summary>
    public static readonly ValueConverter<LocalDate, DateOnly> LocalDateConverter = new(
        v => new DateOnly(v.Year, v.Month, v.Day),
        v => new LocalDate(v.Year, v.Month, v.Day));

    /// <summary>
    ///     A local time, stored as an H2 TIME.
    /// </summary>
    public static readonly ValueConverter<LocalTime, TimeOnly> LocalTimeConverter = new(
        v => TimeOnly.FromTimeSpan(TimeSpan.FromTicks(v.TickOfDay)),
        v => LocalTime.FromTicksSinceMidnight(v.Ticks));

    /// <summary>
    ///     A local date and time, stored as an H2 TIMESTAMP.
    /// </summary>
    public static readonly ValueConverter<LocalDateTime, DateTime> LocalDateTimeConverter = new(
        v => v.ToDateTimeUnspecified(),
        v => LocalDateTime.FromDateTime(v));

    /// <summary>
    ///     An instant, stored as an H2 TIMESTAMP WITH TIME ZONE.
    /// </summary>
    public static readonly ValueConverter<Instant, DateTimeOffset> InstantConverter = new(
        v => v.ToDateTimeOffset(),
        v => Instant.FromDateTimeOffset(v));

    /// <summary>
    ///     A date and time with an offset, stored as an H2 TIMESTAMP WITH TIME ZONE.
    /// </summary>
    public static readonly ValueConverter<OffsetDateTime, DateTimeOffset> OffsetDateTimeConverter = new(
        v => v.ToDateTimeOffset(),
        v => OffsetDateTime.FromDateTimeOffset(v));

    /// <summary>
    ///     A date and time with a zone, stored as an H2 TIMESTAMP WITH TIME ZONE.
    /// </summary>
    /// <remarks>
    ///     The zone is not stored, and cannot be: H2's TIMESTAMP WITH TIME ZONE holds an offset, not a
    ///     zone, which is the same thing PostgreSQL does. The offset is kept, so a value read back
    ///     differs from the one written by exactly the zone.
    /// </remarks>
    public static readonly ValueConverter<ZonedDateTime, DateTimeOffset> ZonedDateTimeConverter = new(
        v => v.ToDateTimeOffset(),
        v => ZonedDateTime.FromDateTimeOffset(v));

    /// <summary>
    ///     A duration, stored as an H2 INTERVAL.
    /// </summary>
    public static readonly ValueConverter<Duration, TimeSpan> DurationConverter = new(
        v => v.ToTimeSpan(),
        v => Duration.FromTimeSpan(v));

    /// <summary>
    ///     A time zone, stored as its identifier.
    /// </summary>
    /// <remarks>
    ///     H2 has no column type for a time zone, and PostgreSQL has none either -- it keeps the IANA
    ///     identifier as text. The text lands in a CLOB here, because that is what the main provider
    ///     maps a string onto.
    /// </remarks>
    public static readonly ValueConverter<DateTimeZone, string> DateTimeZoneConverter = new(
        v => v.Id,
        v => DateTimeZoneProviders.Tzdb[v]);
}
