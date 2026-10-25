using H2Sharp.EntityFrameworkCore.Query.Expressions;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2DateTimeMethodTranslator : IMethodCallTranslator
{
    private static readonly Dictionary<MethodInfo, string> MethodInfoDatePartMapping = new()
    {
        { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddYears), [typeof(int)])!, "YEAR" },
        { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMonths), [typeof(int)])!, "MONTH" },
        { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddDays), [typeof(double)])!, "DAY" },
        { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddHours), [typeof(double)])!, "HOUR" },
        { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMinutes), [typeof(double)])!, "MINUTE" },
        { typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddSeconds), [typeof(double)])!, "SECOND" },
        //{ typeof(DateTime).GetRuntimeMethod(nameof(DateTime.AddMilliseconds), new[] { typeof(double) })!, "milliseconds" },

        { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddYears), [typeof(int)])!, "YEAR" },
        { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMonths), [typeof(int)])!, "MONTH" },
        { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddDays), [typeof(double)])!, "DAY" },
        { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddHours), [typeof(double)])!, "HOUR" },
        { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMinutes), [typeof(double)])!, "MINUTE" },
        { typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddSeconds), [typeof(double)])!, "SECOND" },
        //{ typeof(DateTimeOffset).GetRuntimeMethod(nameof(DateTimeOffset.AddMilliseconds), new[] { typeof(double) })!, "milliseconds" }

        // DateOnly.AddDays, AddMonths and AddYears have a specialized translation, see below
        { typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.AddHours), [typeof(int)])!, "HOUR" },
        { typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.AddMinutes), [typeof(int)])!, "MINUTE" },
    };

    // ReSharper disable InconsistentNaming
    private static readonly MethodInfo DateTime_ToUniversalTime
        = typeof(DateTime).GetRuntimeMethod(nameof(DateTime.ToUniversalTime), [])!;

    private static readonly MethodInfo DateTime_ToLocalTime
        = typeof(DateTime).GetRuntimeMethod(nameof(DateTime.ToLocalTime), [])!;

    private static readonly MethodInfo DateTime_SpecifyKind
        = typeof(DateTime).GetRuntimeMethod(nameof(DateTime.SpecifyKind), [typeof(DateTime), typeof(DateTimeKind)])!;


    private static readonly MethodInfo DateOnly_FromDateTime
        = typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.FromDateTime), [typeof(DateTime)])!;

    private static readonly MethodInfo DateOnly_ToDateTime
        = typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.ToDateTime), [typeof(TimeOnly)])!;


    private static readonly MethodInfo DateOnly_AddDays
        = typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.AddDays), [typeof(int)])!;

    private static readonly MethodInfo DateOnly_AddMonths
        = typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.AddMonths), [typeof(int)])!;

    private static readonly MethodInfo DateOnly_AddYears
        = typeof(DateOnly).GetRuntimeMethod(nameof(DateOnly.AddYears), [typeof(int)])!;

    private static readonly MethodInfo DateOnly_FromDayNumber
        = typeof(DateOnly).GetRuntimeMethod(
            nameof(DateOnly.FromDayNumber), [typeof(int)])!;

    private static readonly MethodInfo TimeOnly_FromDateTime
        = typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.FromDateTime), [typeof(DateTime)])!;

    private static readonly MethodInfo TimeOnly_FromTimeSpan
        = typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.FromTimeSpan), [typeof(TimeSpan)])!;

    private static readonly MethodInfo TimeOnly_ToTimeSpan
        = typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.ToTimeSpan), Type.EmptyTypes)!;

    private static readonly MethodInfo TimeOnly_IsBetween
        = typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.IsBetween), [typeof(TimeOnly), typeof(TimeOnly)])!;

    private static readonly MethodInfo TimeOnly_Add_TimeSpan
        = typeof(TimeOnly).GetRuntimeMethod(nameof(TimeOnly.Add), [typeof(TimeSpan)])!;

    private static readonly MethodInfo TimeZoneInfo_ConvertTimeBySystemTimeZoneId_DateTime
        = typeof(TimeZoneInfo).GetRuntimeMethod(
            nameof(TimeZoneInfo.ConvertTimeBySystemTimeZoneId), [typeof(DateTime), typeof(string)])!;

    private static readonly MethodInfo TimeZoneInfo_ConvertTimeBySystemTimeZoneId_DateTimeOffset
        = typeof(TimeZoneInfo).GetRuntimeMethod(
            nameof(TimeZoneInfo.ConvertTimeBySystemTimeZoneId), [typeof(DateTimeOffset), typeof(string)])!;

    private static readonly MethodInfo TimeZoneInfo_ConvertTimeToUtc
        = typeof(TimeZoneInfo).GetRuntimeMethod(nameof(TimeZoneInfo.ConvertTimeToUtc), [typeof(DateTime)])!;
    // ReSharper restore InconsistentNaming

    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly RelationalTypeMapping _timestampMapping;
    private readonly RelationalTypeMapping _timestampTzMapping;
    private readonly RelationalTypeMapping _intervalMapping;
    private readonly RelationalTypeMapping _textMapping;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2DateTimeMethodTranslator(
        IRelationalTypeMappingSource typeMappingSource,
        H2SqlExpressionFactory sqlExpressionFactory)
    {
        _typeMappingSource = typeMappingSource;
        _sqlExpressionFactory = sqlExpressionFactory;
        _timestampMapping = typeMappingSource.FindMapping(typeof(DateTime), "timestamp without time zone")!;
        _timestampTzMapping = typeMappingSource.FindMapping(typeof(DateTime), "timestamp with time zone")!;
        _intervalMapping = typeMappingSource.FindMapping(typeof(TimeSpan), "interval")!;
        _textMapping = typeMappingSource.FindMapping("text")!;
    }

    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        => TranslateDateTime(instance, method, arguments)
           ?? TranslateDateOnly(instance, method, arguments)
           ?? TranslateTimeOnly(instance, method, arguments)
           ?? TranslateTimeZoneInfo(method, arguments)
           ?? TranslateDatePart(instance, method, arguments);

    private SqlExpression? TranslateDatePart(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments)
        => instance is not null
            && MethodInfoDatePartMapping.TryGetValue(method, out var datePart)
                ? TimestampAdd(instance, arguments[0], datePart)
                : null;

    private SqlExpression? TranslateDateTime(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments)
    {
        if (instance is null)
        {
            if (method == DateTime_SpecifyKind)
            {
                if (arguments[1] is not SqlConstantExpression { Value: DateTimeKind kind })
                {
                    throw new InvalidOperationException("Translating SpecifyKind is only supported with a constant Kind argument");
                }

                var typeMapping = arguments[0].TypeMapping;

                if (typeMapping is not H2TimestampTypeMapping and not H2TimestampTzTypeMapping)
                {
                    throw new InvalidOperationException("Translating SpecifyKind is only supported on timestamp/timestamptz columns");
                }

                if (kind == DateTimeKind.Utc)
                {
                    return typeMapping is H2TimestampTypeMapping
                        ? _sqlExpressionFactory.AtUtc(arguments[0])
                        : arguments[0];
                }

                if (kind is DateTimeKind.Unspecified or DateTimeKind.Local)
                {
                    return typeMapping is H2TimestampTzTypeMapping
                        ? _sqlExpressionFactory.AtUtc(arguments[0])
                        : arguments[0];
                }
            }

        }
        else
        {
            if (method == DateTime_ToUniversalTime)
            {
                return _sqlExpressionFactory.Convert(instance, method.ReturnType, _timestampTzMapping);
            }

            if (method == DateTime_ToLocalTime)
            {
                return _sqlExpressionFactory.Convert(instance, method.ReturnType, _timestampMapping);
            }
        }

        return null;
    }

    private SqlExpression? TranslateDateOnly(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments)
    {
        if (instance is null)
        {
            if (method == DateOnly_FromDateTime)
            {
                // Note: converting timestamptz to date performs a timezone conversion, which is not what .NET DateOnly.FromDateTime does.
                // So if our operand is a timestamptz, we first change the type to timestamp with AT TIME ZONE 'UTC' (returns the same value
                // but as a timestamptz).
                // If our operand is already timestamp, no need to do anything. We throw for anything else to avoid accidentally applying
                // AT TIME ZONE to a non-timestamptz, which would do a timezone conversion
                var dateTime = arguments[0].TypeMapping switch
                {
                    H2TimestampTypeMapping => arguments[0],
                    H2TimestampTzTypeMapping => _sqlExpressionFactory.AtUtc(arguments[0]),
                    _ => throw new NotSupportedException("Can only apply TimeOnly.FromDateTime on a timestamp or timestamptz column")
                };

                return _sqlExpressionFactory.Convert(dateTime, typeof(DateOnly), _typeMappingSource.FindMapping(typeof(DateOnly)));
            }


            if (method == DateOnly_FromDayNumber)
            {
                // We use fragment rather than a DateOnly constant, since 0001-01-01 gets rendered as -infinity by default.
                // TODO: Set the right type/type mapping after https://github.com/dotnet/efcore/pull/34995 is merged
                return _sqlExpressionFactory.Function(
                    "TIMESTAMPADD",
                    [_sqlExpressionFactory.Constant("DAY"), _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]),
                        _sqlExpressionFactory.Fragment("DATE '0001-01-01'")],
                    nullable: true,
                    argumentsPropagateNullability: [false, true, false],
                    typeof(DateOnly),
                    _typeMappingSource.FindMapping(typeof(DateOnly)));
            }
        }
        else
        {
            if (method == DateOnly_ToDateTime)
            {
                return new SqlBinaryExpression(
                    ExpressionType.Add,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(instance),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]),
                    typeof(DateTime),
                    _timestampMapping);
            }

            if (method == DateOnly_AddDays)
            {
                return TimestampAdd(instance, arguments[0], "DAY");
            }

            if (method == DateOnly_AddMonths)
            {
                return TimestampAdd(instance, arguments[0], "MONTH");
            }

            if (method == DateOnly_AddYears)
            {
                return TimestampAdd(instance, arguments[0], "YEAR");
            }
        }

        return null;
    }

    private SqlExpression? TranslateTimeOnly(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments)
    {
        if (method == TimeOnly_FromDateTime)
        {
            // Note: converting timestamptz to time performs a timezone conversion, which is not what .NET TimeOnly.FromDateTime does.
            // So if our operand is a timestamptz, we first change the type to timestamp with AT TIME ZONE 'UTC' (returns the same value
            // but as a timestamptz).
            // If our operand is already timestamp, no need to do anything. We throw for anything else to avoid accidentally applying
            // AT TIME ZONE to a non-timestamptz, which would do a timezone conversion
            var dateTime = arguments[0].TypeMapping switch
            {
                H2TimestampTypeMapping => arguments[0],
                H2TimestampTzTypeMapping => _sqlExpressionFactory.AtUtc(arguments[0]),
                _ => throw new NotSupportedException("Can only apply TimeOnly.FromDateTime on a timestamp or timestamptz column")
            };

            return _sqlExpressionFactory.Convert(
                dateTime,
                typeof(TimeOnly),
                _typeMappingSource.FindMapping(typeof(TimeOnly)));
        }

        if (method == TimeOnly_FromTimeSpan)
        {
            // H2 has no cast from an interval to a time; a time plus an interval is a time.
            return TimeOfDay(arguments[0], ExpressionType.Add);
        }

        if (instance is not null)
        {
            if (method == TimeOnly_ToTimeSpan)
            {
                // H2 has no cast from a time to an interval; the difference of two times is one.
                return _sqlExpressionFactory.Convert(
                    TimeOfDay(instance, ExpressionType.Subtract),
                    typeof(TimeSpan),
                    _typeMappingSource.FindMapping(typeof(TimeSpan)));
            }

            if (method == TimeOnly_IsBetween)
            {
                return _sqlExpressionFactory.And(
                    _sqlExpressionFactory.GreaterThanOrEqual(instance, arguments[0]),
                    _sqlExpressionFactory.LessThan(instance, arguments[1]));
            }

            if (method == TimeOnly_Add_TimeSpan)
            {
                return _sqlExpressionFactory.Add(instance, arguments[0]);
            }
        }

        return null;
    }

    /// <summary>
    ///     Moves a time around midnight.
    /// </summary>
    /// <remarks>
    ///     H2 reads the difference of two times as an interval and the sum of a time and an
    ///     interval as a time, but it casts neither a time to an interval nor an interval to a
    ///     time, so the midnight carries the conversion.
    /// </remarks>
    private SqlExpression TimeOfDay(SqlExpression operand, ExpressionType operation)
    {
        var midnight = _sqlExpressionFactory.Constant(
            TimeOnly.MinValue, _typeMappingSource.FindMapping(typeof(TimeOnly)));

        return operation == ExpressionType.Add
            ? new SqlBinaryExpression(
                ExpressionType.Add,
                midnight,
                _sqlExpressionFactory.ApplyDefaultTypeMapping(operand),
                typeof(TimeOnly),
                _typeMappingSource.FindMapping(typeof(TimeOnly)))
            : new SqlBinaryExpression(
                ExpressionType.Subtract,
                _sqlExpressionFactory.ApplyDefaultTypeMapping(operand),
                midnight,
                typeof(TimeSpan),
                _typeMappingSource.FindMapping(typeof(TimeSpan)));
    }

    private SqlExpression? TranslateTimeZoneInfo(
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments)
    {
        if (method == TimeZoneInfo_ConvertTimeBySystemTimeZoneId_DateTime)
        {
            var typeMapping = arguments[0].TypeMapping;
            if (typeMapping is null
                || (typeMapping.StoreType != "timestamp with time zone" && typeMapping.StoreType != "timestamptz"))
            {
                throw new InvalidOperationException(
                    "TimeZoneInfo.ConvertTimeBySystemTimeZoneId is only supported on columns with type 'timestamp with time zone'");
            }

            return _sqlExpressionFactory.AtTimeZone(arguments[0], arguments[1], typeof(DateTime), _timestampMapping);
        }

        if (method == TimeZoneInfo_ConvertTimeToUtc)
        {
            var typeMapping = arguments[0].TypeMapping;
            if (typeMapping is null
                || (typeMapping.StoreType != "timestamp without time zone" && typeMapping.StoreType != "timestamp"))
            {
                throw new InvalidOperationException(
                    "TimeZoneInfo.ConvertTimeToUtc) is only supported on columns with type 'timestamp without time zone'");
            }

            return _sqlExpressionFactory.Convert(arguments[0], arguments[0].Type, _timestampTzMapping);
        }

        return null;
    }

    /// <summary>
    ///     Adds to a date or timestamp.
    /// </summary>
    /// <remarks>
    ///     H2 has no interval literal: <c>INTERVAL '1 days'</c> is PostgreSQL's way of saying it, and
    ///     the engine reads neither that nor the <c>CAST(... AS interval)</c> PostgreSQL uses for an
    ///     interval that is not a constant. H2 adds with <c>TIMESTAMPADD</c>, which takes the unit
    ///     as a string and the amount as a number.
    /// </remarks>
    private SqlExpression TimestampAdd(SqlExpression instance, SqlExpression amount, string unit)
        => _sqlExpressionFactory.Function(
            "TIMESTAMPADD",
            [_sqlExpressionFactory.Constant(unit, _textMapping), amount, instance],
            nullable: true,
            argumentsPropagateNullability: TrueArrays[3],
            instance.Type,
            // A member read off the result (AddHours(1).Hour) is translated by the member
            // translator, which needs the type mapping of the value to work: without it the
            // value is refused as "could not be translated".
            instance.TypeMapping ?? _timestampMapping);
}
