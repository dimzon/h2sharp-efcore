using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2TimeSpanMemberTranslator : IMemberTranslator
{
    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2TimeSpanMemberTranslator(ISqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
    }

    private static readonly bool[] FalseTrueArray = [false, true];

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        Check.NotNull(member, nameof(member));
        Check.NotNull(returnType, nameof(returnType));

        if (member.DeclaringType == typeof(TimeSpan) && instance is not null)
        {
            // A difference of two timestamps is counted with DATEDIFF rather than with the
            // "-" operator: H2 reads both, but the engine of this port does not read date and
            // time operations yet, while DATEDIFF is ported. DATEDIFF counts whole unit
            // boundaries, not exact fractions: DATEDIFF('HOUR', ...) of a 90-minute span is 1,
            // where .TotalHours is 1.5. The unit matches the member, so whole aligned spans
            // answer exactly.
            if (instance is SqlBinaryExpression
                {
                    OperatorType: ExpressionType.Subtract,
                    Left: { TypeMapping: H2TimestampTypeMapping or H2TimestampTzTypeMapping },
                    Right: { TypeMapping: H2TimestampTypeMapping or H2TimestampTzTypeMapping }
                } subtract
                && member.Name switch
                {
                    nameof(TimeSpan.TotalDays) => "DAY",
                    nameof(TimeSpan.TotalHours) => "HOUR",
                    nameof(TimeSpan.TotalMinutes) => "MINUTE",
                    nameof(TimeSpan.TotalSeconds) => "SECOND",
                    nameof(TimeSpan.TotalMilliseconds) => "MILLISECOND",
                    _ => null
                } is { } unit)
            {
                return _sqlExpressionFactory.Convert(
                    _sqlExpressionFactory.Function(
                        "DATEDIFF",
                        [_sqlExpressionFactory.Constant(unit), subtract.Right, subtract.Left],
                        nullable: true,
                        argumentsPropagateNullability: [false, true, true],
                        typeof(long)),
                    returnType);
            }

            return member.Name switch
            {
                nameof(TimeSpan.Days) => Extract("day", instance, typeof(int)),
                nameof(TimeSpan.Hours) => Extract("hour", instance, typeof(int)),
                nameof(TimeSpan.Minutes) => Extract("minute", instance, typeof(int)),
                nameof(TimeSpan.Seconds) => Extract("second", instance, typeof(int)),
                nameof(TimeSpan.Milliseconds) => Extract("millisecond", instance, typeof(int)),

                nameof(TimeSpan.TotalDays) => Divide(Epoch(instance), 86400),
                nameof(TimeSpan.TotalHours) => Divide(Epoch(instance), 3600),
                nameof(TimeSpan.TotalMinutes) => Divide(Epoch(instance), 60),
                nameof(TimeSpan.TotalSeconds) => Epoch(instance),
                nameof(TimeSpan.TotalMilliseconds) => Multiply(Epoch(instance), 1000),

                _ => null
            };
        }

        return null;

        // The part of an interval is taken by EXTRACT, which in H2 2.5 is a syntax and not a
        // function: EXTRACT(field FROM value). The reference refuses the call of a function
        // ("expected "FROM"") and the whole of date_part with it ("Function "DATE_PART" not
        // found"). The field may be a word or a string: the reference takes both
        // (EXTRACT(DAY FROM x) and EXTRACT('day' FROM x) answer the same).
        SqlExpression Extract(string field, SqlExpression value, Type type)
            => _sqlExpressionFactory.Function(
                "extract",
                [_sqlExpressionFactory.Constant(field), value],
                nullable: true,
                argumentsPropagateNullability: FalseTrueArray,
                type);

        // The seconds of an interval since the epoch, with the fraction of the second.
        SqlExpression Epoch(SqlExpression value)
            => Extract("epoch", value, typeof(double));

        SqlExpression Divide(SqlExpression value, int divisor)
            => _sqlExpressionFactory.Divide(value, _sqlExpressionFactory.Constant(divisor));

        SqlExpression Multiply(SqlExpression value, int factor)
            => _sqlExpressionFactory.Multiply(value, _sqlExpressionFactory.Constant(factor));
    }
}
