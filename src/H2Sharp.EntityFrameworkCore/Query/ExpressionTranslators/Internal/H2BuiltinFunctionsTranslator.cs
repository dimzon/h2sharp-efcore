using System.Reflection;
using H2Sharp.EntityFrameworkCore.Query.Expressions;
using H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         Translates the H2 built-in functions that <see cref="H2DbFunctionsExtensions" /> offers into
///         calls of the same built-in in SQL.
///     </para>
///     <para>
///         Two of them do not translate one for one, and the difference is worth knowing about.
///         <c>LOCATE</c> takes the substring first and the string second, which is the other way round
///         from every other string function, so the arguments are swapped here. And the part of a date
///         is taken by a function per part -- <c>YEAR</c>, <c>MONTH</c>, <c>DAY</c> -- rather than by
///         one function with a field name, so the field name selects the function.
///     </para>
/// </remarks>
public class H2BuiltinFunctionsTranslator : IMethodCallTranslator
{
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly RelationalTypeMapping _stringTypeMapping;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2BuiltinFunctionsTranslator(
        IRelationalTypeMappingSource typeMappingSource,
        H2SqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _stringTypeMapping = typeMappingSource.FindMapping(typeof(string))!;
    }

    /// <summary>
    ///     The date parts H2 can take out of a date or timestamp, and the function that takes each.
    /// </summary>
    private static readonly Dictionary<string, string> DateParts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["YEAR"] = "YEAR",
        ["MONTH"] = "MONTH",
        ["DAY"] = "DAY",
        ["DAY_OF_MONTH"] = "DAY_OF_MONTH",
        ["DAY_OF_WEEK"] = "DAY_OF_WEEK",
        ["DAY_OF_YEAR"] = "DAY_OF_YEAR",
        ["HOUR"] = "HOUR",
        ["MINUTE"] = "MINUTE",
        ["SECOND"] = "SECOND",
        ["QUARTER"] = "QUARTER",
        ["WEEK"] = "WEEK"
    };

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(H2DbFunctionsExtensions))
        {
            return null;
        }

        // The first argument is the DbFunctions instance, which is not part of the call.
        var args = arguments.Skip(1).ToArray();

        return method.Name switch
        {
            nameof(H2DbFunctionsExtensions.DateAdd)
                => Function("DATEADD", [IntervalUnit(args[0]), args[1], args[2]], method),
            nameof(H2DbFunctionsExtensions.DateDiff)
                => Function("DATEDIFF", [IntervalUnit(args[0]), args[1], args[2]], method),
            nameof(H2DbFunctionsExtensions.DateTrunc)
                => Function("DATE_TRUNC", [IntervalUnit(args[0]), args[1]], method),
            nameof(H2DbFunctionsExtensions.Extract)
                => Function(DatePartFunction(args[0]), [args[1]], method),
            nameof(H2DbFunctionsExtensions.DayName)
                => Function("DAYNAME", [args[0]], method),
            nameof(H2DbFunctionsExtensions.MonthName)
                => Function("MONTHNAME", [args[0]], method),
            nameof(H2DbFunctionsExtensions.Locate)
                => Locate(args, method),
            nameof(H2DbFunctionsExtensions.LeftPad)
                => Function("LPAD", [args[0], args[1], PadString(args[2])], method),
            nameof(H2DbFunctionsExtensions.RightPad)
                => Function("RPAD", [args[0], args[1], PadString(args[2])], method),
            nameof(H2DbFunctionsExtensions.Translate)
                => Function("TRANSLATE", [args[0], args[1], args[2]], method),
            nameof(H2DbFunctionsExtensions.Repeat)
                => Function("REPEAT", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.Space)
                => Function("SPACE", [args[0]], method),
            nameof(H2DbFunctionsExtensions.QuoteIdentifier)
                => Function("QUOTE_IDENT", [args[0]], method),
            nameof(H2DbFunctionsExtensions.Soundex)
                => Function("SOUNDEX", [args[0]], method),
            nameof(H2DbFunctionsExtensions.SoundexDifference)
                => Function("DIFFERENCE", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.Sign)
                => Function("SIGN", [args[0]], method),
            // Truncate has an overload with a precision, and H2 has a function for each: TRUNC
            // drops the fraction, TRUNCATE keeps a number of decimal places.
            "Truncate"
                => args.Length == 1
                    ? Function("TRUNC", [args[0]], method)
                    : Function("TRUNCATE", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.BitAnd)
                => Function("BITAND", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.BitOr)
                => Function("BITOR", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.BitXor)
                => Function("BITXOR", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.Ascii)
                => Function("ASCII", [args[0]], method),
            nameof(H2DbFunctionsExtensions.Char)
                => Function("CHAR", [args[0]], method),
            nameof(H2DbFunctionsExtensions.Chr)
                => Function("CHR", [args[0]], method),
            nameof(H2DbFunctionsExtensions.StringDecode)
                => Function("STRINGDECODE", [args[0]], method),
            nameof(H2DbFunctionsExtensions.StringEncode)
                => Function("STRINGENCODE", [args[0]], method),
            nameof(H2DbFunctionsExtensions.StringToUtf8)
                => Function("STRINGTOUTF8", [args[0]], method),
            nameof(H2DbFunctionsExtensions.Utf8ToString)
                => Function("UTF8TOSTRING", [args[0]], method),
            nameof(H2DbFunctionsExtensions.HexToRaw)
                => Function("HEXTORAW", [args[0]], method),
            nameof(H2DbFunctionsExtensions.RawToHex)
                => Function("RAWTOHEX", [args[0]], method),
            nameof(H2DbFunctionsExtensions.BitNot)
                => Function("BITNOT", [args[0]], method),
            nameof(H2DbFunctionsExtensions.BitNand)
                => Function("BITNAND", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.BitNor)
                => Function("BITNOR", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.BitXnor)
                => Function("BITXNOR", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.BitGet)
                => Function("BITGET", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.BitCount)
                => Function("BITCOUNT", [args[0]], method),
            nameof(H2DbFunctionsExtensions.LeftShift)
                => Function("LSHIFT", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.RightShift)
                => Function("RSHIFT", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.UnsignedLeftShift)
                => Function("ULSHIFT", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.UnsignedRightShift)
                => Function("URSHIFT", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.RotateLeft)
                => Function("ROTATELEFT", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.RotateRight)
                => Function("ROTATERIGHT", [args[0], args[1]], method),
            nameof(H2DbFunctionsExtensions.Cotangent)
                => Function("COT", [args[0]], method),
            nameof(H2DbFunctionsExtensions.RoundMagic)
                => Function("ROUNDMAGIC", [args[0]], method),
            nameof(H2DbFunctionsExtensions.Insert)
                => Function("INSERT", [args[0], args[1], args[2], args[3]], method),
            nameof(H2DbFunctionsExtensions.Compress)
                => Function("COMPRESS", [args[0]], method),
            nameof(H2DbFunctionsExtensions.Expand)
                => Function("EXPAND", [args[0]], method),
            nameof(H2DbFunctionsExtensions.ConcatWithSeparator)
                => Function("CONCAT_WS", [args[0], args[1], args[2], .. args.Skip(3)], method),
            _ => null
        };
    }

    private SqlExpression Function(string name, IReadOnlyList<SqlExpression> arguments, MethodInfo method)
        => _sqlExpressionFactory.Function(
            name,
            arguments,
            nullable: true,
            argumentsPropagateNullability: TrueArrays[arguments.Count],
            method.ReturnType);

    /// <summary>
    ///     <c>LOCATE</c> takes the substring first. A start position of zero means "from the
    ///     beginning", which is what the two-argument form says.
    /// </summary>
    private SqlExpression Locate(IReadOnlyList<SqlExpression> args, MethodInfo method)
    {
        var (value, substring, startPosition) = (args[0], args[1], args[2]);

        var start = startPosition is SqlConstantExpression { Value: int position }
            ? position
            : throw new InvalidOperationException(
                "The start position of Locate must be a constant, because H2 needs it to decide " +
                "whether to search the whole string.");

        return start <= 0
            ? Function("LOCATE", [substring, value], method)
            : Function("LOCATE", [substring, value, startPosition], method);
    }

    /// <summary>
    ///     The unit of an interval, which H2 takes as a string.
    /// </summary>
    private SqlExpression IntervalUnit(SqlExpression expression)
        => expression is SqlConstantExpression { Value: string unit }
            ? _sqlExpressionFactory.Constant(unit, _stringTypeMapping)
            : throw new InvalidOperationException("The unit of an interval must be a constant.");

    /// <summary>
    ///     The part of a date to take out, which selects the function that takes it.
    /// </summary>
    private string DatePartFunction(SqlExpression expression)
    {
        if (expression is not SqlConstantExpression { Value: string field }
            || !DateParts.TryGetValue(field, out var function))
        {
            throw new InvalidOperationException(
                $"The part of a date must be one of: {string.Join(", ", DateParts.Keys)}.");
        }

        return function;
    }

    /// <summary>
    ///     The string to pad with, which H2 takes as a string.
    /// </summary>
    private SqlExpression PadString(SqlExpression expression)
        => expression is SqlConstantExpression { Value: string pad }
            ? _sqlExpressionFactory.Constant(pad, _stringTypeMapping)
            : throw new InvalidOperationException("The pad string must be a constant.");
}
