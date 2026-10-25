namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

using System.Text;

/// <summary>
///     Writes a string literal the way H2 accepts it: <c>E'...'</c> when the text has only printable
///     ASCII, <c>U&amp;'...'</c> otherwise. H2 decodes no escape sequences in an E-string, so it is the
///     plain literal with an E in front, and it has no <c>EU&amp;'...'</c> form.
///     This is a "virtual" type mapping which is never returned by <see cref="H2TypeMappingSource" />.
///     It is only used internally by some method translators to produce literal strings.
/// </summary>
/// <remarks>
///     See https://www.h2database.com/html/grammar.html
/// </remarks>
public class H2EStringTypeMapping : StringTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public new static H2EStringTypeMapping Default { get; } = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2EStringTypeMapping()
        : base("does_not_exist", System.Data.DbType.String)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
    {
        var text = (string)value;
        var builder = new StringBuilder(text.Length + 2);

        // H2's E-string is a plain literal with an E in front: it decodes no escape sequences.
        // A text with a control or non-ASCII character travels as U&'...', and H2 has no
        // EU&'...' form, so such a text takes the plain prefix instead.
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] < ' ' || text[i] > (char)127)
            {
                return Dialect.AppendString(builder, text).ToString();
            }
        }

        return Dialect.AppendEString(builder, text).ToString();
    }
}
