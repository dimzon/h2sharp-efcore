using System.Reflection;
using H2Sharp.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime;

namespace H2Sharp.EntityFrameworkCore.NodaTime.Query.Internal;

/// <summary>
///     A plugin that brings the NodaTime member translations into the composite member translator.
/// </summary>
public class H2NodaTimeMemberTranslatorPlugin(IEnumerable<IMemberTranslator> translators) : IMemberTranslatorPlugin
{
    /// <summary>
    ///     The translators this plugin contributes.
    /// </summary>
    public virtual IEnumerable<IMemberTranslator> Translators { get; } = translators;
}

/// <summary>
///     Translates date and time members of the NodaTime types onto the H2 part functions.
/// </summary>
/// <remarks>
///     <para>
///         Only the types whose H2 column carries no time zone are translated: <see cref="LocalDate" />
///         over <c>date</c>, <see cref="LocalTime" /> over <c>time</c> and <see cref="LocalDateTime" />
///         over <c>timestamp</c>. Their fields are the fields of the value as it was written, which is
///         what the part functions of H2 answer for these column types.
///     </para>
///     <para>
///         <see cref="OffsetDateTime" /> and <see cref="ZonedDateTime" /> are not translated: they are
///         stored as <c>timestamp with time zone</c>, and the part functions of H2 read such a value in
///         the zone of the session, while NodaTime asks for the fields of the value's own offset.
///         Returning <see langword="null" /> makes EF Core say the member cannot be translated, which
///         is honest, instead of answering with fields of some other zone.
///     </para>
///     <para>
    ///         <see cref="LocalDate.DayOfWeek" /> is <see cref="IsoDayOfWeek" />, which numbers the
    ///         week from Monday the way the ISO calendar does, so <c>ISO_DAY_OF_WEEK</c> answers it with no
    ///         arithmetic. This differs from the BCL <see cref="DateTime.DayOfWeek" />, which numbers from
    ///         Sunday and needs the remainder by seven.
    ///     </para>
    /// </remarks>
public class H2NodaTimeMemberTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMemberTranslator
{
    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        var declaringType = member.DeclaringType;

        if (declaringType != typeof(LocalDate)
            && declaringType != typeof(LocalTime)
            && declaringType != typeof(LocalDateTime))
        {
            return null;
        }

        var functionName = member.Name switch
        {
            nameof(LocalDate.Year) => "year",
            nameof(LocalDate.Month) => "month",
            nameof(LocalDate.Day) => "day",
            nameof(LocalDate.DayOfYear) => "day_of_year",
            nameof(LocalDate.DayOfWeek) => "iso_day_of_week",

            nameof(LocalTime.Hour) => "hour",
            nameof(LocalTime.Minute) => "minute",
            nameof(LocalTime.Second) => "second",

            _ => null,
        };

        if (functionName is null || instance is null)
        {
            return null;
        }

        return sqlExpressionFactory.Function(
            functionName,
            [instance],
            nullable: true,
            argumentsPropagateNullability: [true],
            typeof(int));
    }
}
