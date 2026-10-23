using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Provides H2-specific extension methods on <see cref="DbFunctions" />.
/// </summary>
/// <remarks>
///     <para>
///         Every method here is translated into a call of the H2 built-in of the same name, and
///         every one of them was checked against the engine: a method that the engine cannot run
///         is not offered at all, because a method that only works on the client is a trap.
///     </para>
///     <para>
///         The H2 built-ins the engine cannot run yet -- <c>TO_CHAR</c>, <c>FORMATDATETIME</c>,
///         <c>PARSEDATETIME</c>, <c>REGEXP_LIKE</c> and the other regular-expression functions,
///         <c>HASH</c>, <c>JSON_OBJECT</c>, <c>JSON_ARRAY</c>, the session and system functions --
///         are not here either. When the engine grows them, they belong here.
///     </para>
/// </remarks>
public static class H2DbFunctionsExtensions
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    ///     An implementation of the H2 <c>ILIKE</c> operation, which is an insensitive LIKE.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="matchExpression">The string that is to be matched.</param>
    /// <param name="pattern">The pattern which may involve wildcards %,_,[,],^.</param>
    /// <returns><see langword="true" /> if there is a match.</returns>
    public static bool ILike(this DbFunctions _, string matchExpression, string pattern)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(ILike)));

    // ReSharper disable once InconsistentNaming
    /// <summary>
    ///     An implementation of the H2 <c>ILIKE</c> operation, which is an insensitive LIKE.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="matchExpression">The string that is to be matched.</param>
    /// <param name="pattern">The pattern which may involve wildcards %,_,[,],^.</param>
    /// <param name="escapeCharacter">
    ///     The escape character (as a single character string) to use in front of %,_,[,],^
    ///     if they are not used as wildcards.
    /// </param>
    /// <returns><see langword="true" /> if there is a match.</returns>
    public static bool ILike(this DbFunctions _, string matchExpression, string pattern, string escapeCharacter)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(ILike)));

    /// <summary>
    ///     Returns whether the row value represented by <paramref name="a" /> is greater than the row value represented by
    ///     <paramref name="b" />.
    /// </summary>
    /// <remarks>
    ///     For more information on row value comparisons, see
    ///     <see href="https://www.h2database.com/html/sqlCommands.html#rowValueComparison">
    ///         the H2 documentation.
    ///     </see>
    /// </remarks>
    public static bool GreaterThan(this DbFunctions _, ITuple a, ITuple b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(GreaterThan)));

    /// <summary>
    ///     Returns whether the row value represented by <paramref name="a" /> is less than the row value represented by
    ///     <paramref name="b" />.
    /// </summary>
    /// <remarks>
    ///     For more information on row value comparisons, see
    ///     <see href="https://www.h2database.com/html/sqlCommands.html#rowValueComparison">
    ///         the H2 documentation.
    ///     </see>
    /// </remarks>
    public static bool LessThan(this DbFunctions _, ITuple a, ITuple b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(LessThan)));

    /// <summary>
    ///     Returns whether the row value represented by <paramref name="a" /> is greater than or equal to the row value
    ///     represented by <paramref name="b" />.
    /// </summary>
    /// <remarks>
    ///     For more information on row value comparisons, see
    ///     <see href="https://www.h2database.com/html/sqlCommands.html#rowValueComparison">
    ///         the H2 documentation.
    ///     </see>
    /// </remarks>
    public static bool GreaterThanOrEqual(this DbFunctions _, ITuple a, ITuple b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(GreaterThanOrEqual)));

    /// <summary>
    ///     Returns whether the row value represented by <paramref name="a" /> is less than or equal to the row value
    ///     represented by <paramref name="b" />.
    /// </summary>
    /// <remarks>
    ///     For more information on row value comparisons, see
    ///     <see href="https://www.h2database.com/html/sqlCommands.html#rowValueComparison">
    ///         the H2 documentation.
    ///     </see>
    /// </remarks>
    public static bool LessThanOrEqual(this DbFunctions _, ITuple a, ITuple b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(LessThanOrEqual)));

    /// <summary>
    ///     Returns whether the row value represented by <paramref name="a" /> is equal to the row value
    ///     represented by <paramref name="b" />.
    /// </summary>
    /// <remarks>
    ///     For more information on row value comparisons, see
    ///     <see href="https://www.h2database.com/html/sqlCommands.html#rowValueComparison">
    ///         the H2 documentation.
    ///     </see>
    /// </remarks>
    public static bool Equal(this DbFunctions _, ITuple a, ITuple b)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Equal)));

    /// <summary>
    ///     Adds an amount of time to a date or timestamp, the way H2's <c>DATEADD</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="unit">
    ///     The unit of the interval: <c>YEAR</c>, <c>MONTH</c>, <c>DAY</c>, <c>HOUR</c>, <c>MINUTE</c>,
    ///     <c>SECOND</c> or <c>WEEK</c>.
    /// </param>
    /// <param name="count">How many units to add. May be negative.</param>
    /// <param name="value">The date or timestamp to add to.</param>
    /// <returns>The date or timestamp moved by <paramref name="count" /> <paramref name="unit" />s.</returns>
    public static DateTime DateAdd(this DbFunctions _, string unit, int count, DateTime value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(DateAdd)));

    /// <summary>
    ///     Counts how many <paramref name="unit" />s lie between two dates or timestamps, the way H2's
    ///     <c>DATEDIFF</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="unit">
    ///     The unit of the interval: <c>YEAR</c>, <c>MONTH</c>, <c>DAY</c>, <c>HOUR</c>, <c>MINUTE</c>,
    ///     <c>SECOND</c> or <c>WEEK</c>.
    /// </param>
    /// <param name="start">The date or timestamp the interval starts at.</param>
    /// <param name="end">The date or timestamp the interval ends at.</param>
    /// <returns>The number of whole units between the two.</returns>
    public static long DateDiff(this DbFunctions _, string unit, DateTime start, DateTime end)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(DateDiff)));

    /// <summary>
    ///     Truncates a date or timestamp to the given unit, the way H2's <c>DATE_TRUNC</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="unit">
    ///     The unit to truncate to: <c>YEAR</c>, <c>MONTH</c>, <c>DAY</c>, <c>HOUR</c>, <c>MINUTE</c>,
    ///     <c>SECOND</c> or <c>WEEK</c>.
    /// </param>
    /// <param name="value">The date or timestamp to truncate.</param>
    /// <returns>The truncated date or timestamp.</returns>
    public static DateTime DateTrunc(this DbFunctions _, string unit, DateTime value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(DateTrunc)));

    /// <summary>
    ///     Takes one part out of a date or timestamp, the way H2's <c>YEAR</c>, <c>MONTH</c>, <c>DAY</c>,
    ///     <c>HOUR</c>, <c>MINUTE</c>, <c>SECOND</c> and the other part functions do.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="field">
    ///     The part to take: <c>YEAR</c>, <c>MONTH</c>, <c>DAY</c>, <c>DAY_OF_MONTH</c>, <c>DAY_OF_WEEK</c>,
    ///     <c>DAY_OF_YEAR</c>, <c>HOUR</c>, <c>MINUTE</c>, <c>SECOND</c>, <c>QUARTER</c> or <c>WEEK</c>.
    /// </param>
    /// <param name="value">The date or timestamp to take the part from.</param>
    /// <returns>The part, as a number.</returns>
    public static int Extract(this DbFunctions _, string field, DateTime value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Extract)));

    /// <summary>
    ///     Returns the name of the day of the week, the way H2's <c>DAYNAME</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The date or timestamp.</param>
    /// <returns>The name of the day of the week, in English.</returns>
    public static string DayName(this DbFunctions _, DateTime value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(DayName)));

    /// <summary>
    ///     Returns the name of the month, the way H2's <c>MONTHNAME</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The date or timestamp.</param>
    /// <returns>The name of the month, in English.</returns>
    public static string MonthName(this DbFunctions _, DateTime value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(MonthName)));

    /// <summary>
    ///     Returns the position of a substring in a string, the way H2's <c>LOCATE</c> does: one-based,
    ///     and zero when the substring is not there.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to search in.</param>
    /// <param name="substring">The substring to search for.</param>
    /// <param name="startPosition">
    ///     The one-based position to start searching at. Zero or less means the beginning of the string.
    /// </param>
    /// <returns>The one-based position of the substring, or zero if it is not there.</returns>
    public static int Locate(this DbFunctions _, string value, string substring, int startPosition = 0)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Locate)));

    /// <summary>
    ///     Pads a string on the left up to a given length, the way H2's <c>LPAD</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to pad.</param>
    /// <param name="length">The length to pad to. A string longer than this is truncated.</param>
    /// <param name="padString">The string to pad with. Defaults to a space.</param>
    /// <returns>The padded string.</returns>
    public static string LeftPad(this DbFunctions _, string value, int length, string padString = " ")
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(LeftPad)));

    /// <summary>
    ///     Pads a string on the right up to a given length, the way H2's <c>RPAD</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to pad.</param>
    /// <param name="length">The length to pad to. A string longer than this is truncated.</param>
    /// <param name="padString">The string to pad with. Defaults to a space.</param>
    /// <returns>The padded string.</returns>
    public static string RightPad(this DbFunctions _, string value, int length, string padString = " ")
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(RightPad)));

    /// <summary>
    ///     Replaces every character of a string with the character at the same position of another
    ///     string, the way H2's <c>TRANSLATE</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to translate.</param>
    /// <param name="from">The characters to replace.</param>
    /// <param name="to">
    ///     The characters to replace them with. A shorter string means the extra characters in
    ///     <paramref name="from" /> are removed.
    /// </param>
    /// <returns>The translated string.</returns>
    public static string Translate(this DbFunctions _, string value, string from, string to)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Translate)));

    /// <summary>
    ///     Repeats a string a number of times, the way H2's <c>REPEAT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to repeat.</param>
    /// <param name="count">How many times to repeat it.</param>
    /// <returns>The repeated string.</returns>
    public static string Repeat(this DbFunctions _, string value, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Repeat)));

    /// <summary>
    ///     Returns a string of spaces, the way H2's <c>SPACE</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="count">How many spaces.</param>
    /// <returns>The string of spaces.</returns>
    public static string Space(this DbFunctions _, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Space)));

    /// <summary>
    ///     Quotes an identifier the way H2 does, the way H2's <c>QUOTE_IDENT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="identifier">The identifier to quote.</param>
    /// <returns>The quoted identifier.</returns>
    public static string QuoteIdentifier(this DbFunctions _, string identifier)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(QuoteIdentifier)));

    /// <summary>
    ///     Returns the soundex code of a string, the way H2's <c>SOUNDEX</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to code.</param>
    /// <returns>The soundex code.</returns>
    public static string Soundex(this DbFunctions _, string value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Soundex)));

    /// <summary>
    ///     Returns how alike two strings sound, the way H2's <c>DIFFERENCE</c> does: zero means the same.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="first">The first string.</param>
    /// <param name="second">The second string.</param>
    /// <returns>The difference between the soundex codes of the two strings.</returns>
    public static int SoundexDifference(this DbFunctions _, string first, string second)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(SoundexDifference)));

    /// <summary>
    ///     Returns the sign of a number, the way H2's <c>SIGN</c> does: minus one, zero or one.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <returns>The sign of the number.</returns>
    public static int Sign(this DbFunctions _, decimal value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Sign)));

    /// <summary>
    ///     Truncates a number to a number of decimal places, the way H2's <c>TRUNCATE</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number to truncate.</param>
    /// <param name="precision">
    ///     The number of decimal places to keep. A negative number truncates to tens, hundreds and so on.
    /// </param>
    /// <returns>The truncated number.</returns>
    public static decimal Truncate(this DbFunctions _, decimal value, int precision)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Truncate)));

    /// <summary>
    ///     Returns the bitwise AND of two numbers, the way H2's <c>BITAND</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns>The bitwise AND of the two numbers.</returns>
    public static long BitAnd(this DbFunctions _, long left, long right)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitAnd)));

    /// <summary>
    ///     Returns the bitwise OR of two numbers, the way H2's <c>BITOR</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns>The bitwise OR of the two numbers.</returns>
    public static long BitOr(this DbFunctions _, long left, long right)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitOr)));

    /// <summary>
    ///     Returns the bitwise exclusive OR of two numbers, the way H2's <c>BITXOR</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns>The bitwise exclusive OR of the two numbers.</returns>
    public static long BitXor(this DbFunctions _, long left, long right)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitXor)));

    /// <summary>
    ///     Returns the ASCII code of the first character of a string, the way H2's <c>ASCII</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string.</param>
    /// <returns>The ASCII code of the first character.</returns>
    public static int Ascii(this DbFunctions _, string value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Ascii)));

    /// <summary>
    ///     Returns the character with the given Unicode code point, the way H2's <c>CHAR</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="code">The code point.</param>
    /// <returns>The character.</returns>
    public static string Char(this DbFunctions _, int code)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Char)));

    /// <summary>
    ///     Returns the character with the given Unicode code point, the way H2's <c>CHR</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="code">The code point.</param>
    /// <returns>The character.</returns>
    public static string Chr(this DbFunctions _, int code)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Chr)));

    /// <summary>
    ///     Decodes the escape sequences in a string, the way H2's <c>STRINGDECODE</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to decode.</param>
    /// <returns>The decoded string.</returns>
    public static string StringDecode(this DbFunctions _, string value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(StringDecode)));

    /// <summary>
    ///     Encodes the characters of a string that are outside the basic multilingual plane, the way H2's
    ///     <c>STRINGENCODE</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to encode.</param>
    /// <returns>The encoded string.</returns>
    public static string StringEncode(this DbFunctions _, string value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(StringEncode)));

    /// <summary>
    ///     Returns the UTF-8 bytes of a string, the way H2's <c>STRINGTOUTF8</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string.</param>
    /// <returns>The UTF-8 bytes of the string.</returns>
    public static byte[] StringToUtf8(this DbFunctions _, string value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(StringToUtf8)));

    /// <summary>
    ///     Returns the string that UTF-8 bytes spell, the way H2's <c>UTF8TOSTRING</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The UTF-8 bytes.</param>
    /// <returns>The string the bytes spell.</returns>
    public static string Utf8ToString(this DbFunctions _, byte[] value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Utf8ToString)));

    /// <summary>
    ///     Returns the bytes a hexadecimal string spells, the way H2's <c>HEXTORAW</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="hex">
    ///     The hexadecimal string, with the <c>0x</c> prefix H2 expects: <c>0x414243</c>.
    /// </param>
    /// <returns>The bytes.</returns>
    public static byte[] HexToRaw(this DbFunctions _, string hex)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(HexToRaw)));

    /// <summary>
    ///     Returns the hexadecimal form of bytes, the way H2's <c>RAWTOHEX</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The bytes.</param>
    /// <returns>The hexadecimal form of the bytes.</returns>
    public static string RawToHex(this DbFunctions _, byte[] value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(RawToHex)));

    /// <summary>
    ///     Returns the bitwise NOT of a number, the way H2's <c>BITNOT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <returns>The bitwise NOT of the number.</returns>
    public static long BitNot(this DbFunctions _, long value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitNot)));

    /// <summary>
    ///     Returns the bitwise NOT AND of two numbers, the way H2's <c>BITNAND</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns>The bitwise NOT AND of the two numbers.</returns>
    public static long BitNand(this DbFunctions _, long left, long right)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitNand)));

    /// <summary>
    ///     Returns the bitwise NOT OR of two numbers, the way H2's <c>BITNOR</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns>The bitwise NOT OR of the two numbers.</returns>
    public static long BitNor(this DbFunctions _, long left, long right)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitNor)));

    /// <summary>
    ///     Returns the bitwise NOT XOR of two numbers, the way H2's <c>BITXNOR</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns>The bitwise NOT XOR of the two numbers.</returns>
    public static long BitXnor(this DbFunctions _, long left, long right)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitXnor)));

    /// <summary>
    ///     Returns one bit of a number, the way H2's <c>BITGET</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <param name="bit">The one-based number of the bit.</param>
    /// <returns>Whether the bit is set.</returns>
    public static bool BitGet(this DbFunctions _, long value, int bit)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitGet)));

    /// <summary>
    ///     Returns how many bits are set in a number, the way H2's <c>BITCOUNT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <returns>The number of bits that are set.</returns>
    public static long BitCount(this DbFunctions _, long value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(BitCount)));

    /// <summary>
    ///     Shifts a number left, the way H2's <c>LSHIFT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <param name="count">How many bits to shift by.</param>
    /// <returns>The shifted number.</returns>
    public static long LeftShift(this DbFunctions _, long value, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(LeftShift)));

    /// <summary>
    ///     Shifts a number right, the way H2's <c>RSHIFT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <param name="count">How many bits to shift by.</param>
    /// <returns>The shifted number.</returns>
    public static long RightShift(this DbFunctions _, long value, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(RightShift)));

    /// <summary>
    ///     Shifts a number left, the way H2's <c>ULSHIFT</c> does: the number is treated as unsigned.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <param name="count">How many bits to shift by.</param>
    /// <returns>The shifted number.</returns>
    public static long UnsignedLeftShift(this DbFunctions _, long value, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(UnsignedLeftShift)));

    /// <summary>
    ///     Shifts a number right, the way H2's <c>URSHIFT</c> does: the number is treated as unsigned.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <param name="count">How many bits to shift by.</param>
    /// <returns>The shifted number.</returns>
    public static long UnsignedRightShift(this DbFunctions _, long value, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(UnsignedRightShift)));

    /// <summary>
    ///     Rotates a number left, the way H2's <c>ROTATELEFT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <param name="count">How many bits to rotate by.</param>
    /// <returns>The rotated number.</returns>
    public static long RotateLeft(this DbFunctions _, long value, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(RotateLeft)));

    /// <summary>
    ///     Rotates a number right, the way H2's <c>ROTATERIGHT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number.</param>
    /// <param name="count">How many bits to rotate by.</param>
    /// <returns>The rotated number.</returns>
    public static long RotateRight(this DbFunctions _, long value, int count)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(RotateRight)));

    /// <summary>
    ///     Truncates a number to a whole number, the way H2's <c>TRUNC</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number to truncate.</param>
    /// <returns>The truncated number.</returns>
    public static decimal Truncate(this DbFunctions _, decimal value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Truncate)));

    /// <summary>
    ///     Returns the cotangent of an angle in radians, the way H2's <c>COT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The angle in radians.</param>
    /// <returns>The cotangent.</returns>
    public static double Cotangent(this DbFunctions _, double value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Cotangent)));

    /// <summary>
    ///     Rounds a number to a whole number, the way H2's <c>ROUNDMAGIC</c> does: halves go up.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The number to round.</param>
    /// <returns>The rounded number.</returns>
    public static decimal RoundMagic(this DbFunctions _, decimal value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(RoundMagic)));

    /// <summary>
    ///     Inserts a string into another, the way H2's <c>INSERT</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to insert into.</param>
    /// <param name="position">The one-based position to insert at.</param>
    /// <param name="length">How many characters to replace. A negative length inserts without removing.</param>
    /// <param name="insert">The string to insert.</param>
    /// <returns>The string with the insertion made.</returns>
    public static string Insert(this DbFunctions _, string value, int position, int length, string insert)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Insert)));

    /// <summary>
    ///     Compresses a string, the way H2's <c>COMPRESS</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The string to compress.</param>
    /// <returns>The compressed bytes.</returns>
    public static byte[] Compress(this DbFunctions _, string value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Compress)));

    /// <summary>
    ///     Joins strings with a separator, the way H2's <c>CONCAT_WS</c> does: the separator goes
    ///     between the strings, and a null string contributes nothing, not even a separator.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="separator">The separator to put between the strings.</param>
    /// <param name="first">The first string.</param>
    /// <param name="second">The second string.</param>
    /// <param name="rest">The other strings.</param>
    /// <returns>The joined string.</returns>
    public static string ConcatWithSeparator(this DbFunctions _, string separator, string first, string second, params string[] rest)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(ConcatWithSeparator)));

    /// <summary>
    ///     Takes a compressed string apart again, the way H2's <c>EXPAND</c> does.
    /// </summary>
    /// <param name="_">The <see cref="DbFunctions" /> instance.</param>
    /// <param name="value">The compressed bytes.</param>
    /// <returns>The string the bytes spell.</returns>
    public static string Expand(this DbFunctions _, byte[] value)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(Expand)));}
