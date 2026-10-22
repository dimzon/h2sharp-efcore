using System.Text;
using System.Text.Json;
using System.Data.Common;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     Supports the older H2-specific JSON mapping, allowing mapping json to text, to e.g.
///     <see cref="JsonElement" /> (weakly-typed mapping) or to arbitrary POCOs (but without them being modeled).
///     For the standard EF JSON support, which relies on owned entity modeling, see <see cref="H2OwnedJsonTypeMapping" />.
/// </summary>
public class H2JsonTypeMapping : H2TypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static H2JsonTypeMapping Default { get; } = new("json", typeof(JsonElement));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2JsonTypeMapping(string storeType, Type clrType)
        : base(storeType, clrType)
    {
        // H2 has a single JSON type: no jsonb, and no binary storage format behind it.
        if (storeType != "json")
        {
            throw new ArgumentException($"{nameof(storeType)} must be 'json'", nameof(storeType));
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2JsonTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2JsonTypeMapping(parameters);

    /// <inheritdoc />
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);
        if (parameter.Value is null or DBNull)
        {
            return;
        }

        // H2 parses binary input to JSON as UTF-8 JSON text. Character input is a JSON string,
        // so sending serialized documents as VARCHAR would silently double-encode them.
        parameter.Value = Encoding.UTF8.GetBytes(parameter.Value switch
        {
            JsonDocument document => document.RootElement.GetRawText(),
            JsonElement element => element.GetRawText(),
            string json => json,
            var poco => JsonSerializer.Serialize(poco)
        });
    }

    /// <inheritdoc />
    public override MethodInfo GetDataReaderMethod()
        => typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetString), [typeof(int)])!;

    /// <inheritdoc />
    public override Expression CustomizeDataReaderExpression(Expression expression)
    {
        if (ClrType == typeof(string))
        {
            return expression;
        }
        if (ClrType == typeof(JsonDocument) || ClrType == typeof(JsonElement))
        {
            var document = Expression.Call(ParseMethod, expression, DefaultJsonDocumentOptions);
            return ClrType == typeof(JsonDocument)
                ? document
                : Expression.Property(document, nameof(JsonDocument.RootElement));
        }

        return Expression.Call(typeof(JsonSerializer).GetMethods()
                .Single(m => m.Name == nameof(JsonSerializer.Deserialize) && m.IsGenericMethodDefinition
                    && m.GetParameters() is var p && p.Length == 2 && p[0].ParameterType == typeof(string)
                    && p[1].ParameterType == typeof(JsonSerializerOptions)).MakeGenericMethod(ClrType),
            expression, Expression.Constant(null, typeof(JsonSerializerOptions)));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual string EscapeSqlLiteral(string literal)
        => Check.NotNull(literal, nameof(literal)).Replace("'", "''");

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
    {
        switch (value)
        {
            case JsonDocument _:
            case JsonElement _:
            {
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                if (value is JsonDocument doc)
                {
                    doc.WriteTo(writer);
                }
                else
                {
                    ((JsonElement)value).WriteTo(writer);
                }

                writer.Flush();
                return $"JSON '{EscapeSqlLiteral(Encoding.UTF8.GetString(stream.ToArray()))}'";
            }
            case string s:
                return $"JSON '{EscapeSqlLiteral(s)}'";
            default: // User POCO
                return $"JSON '{EscapeSqlLiteral(JsonSerializer.Serialize(value))}'";
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Expression GenerateCodeLiteral(object value)
        => value switch
        {
            JsonDocument document => Expression.Call(
                ParseMethod, Expression.Constant(document.RootElement.ToString()), DefaultJsonDocumentOptions),
            JsonElement element => Expression.Property(
                Expression.Call(ParseMethod, Expression.Constant(element.ToString()), DefaultJsonDocumentOptions),
                nameof(JsonDocument.RootElement)),
            string s => Expression.Constant(s),
            _ => throw new NotSupportedException("Cannot generate code literals for JSON POCOs")
        };

    private static readonly Expression DefaultJsonDocumentOptions = Expression.New(typeof(JsonDocumentOptions));

    private static readonly MethodInfo ParseMethod =
        typeof(JsonDocument).GetMethod(nameof(JsonDocument.Parse), [typeof(string), typeof(JsonDocumentOptions)])!;
}
