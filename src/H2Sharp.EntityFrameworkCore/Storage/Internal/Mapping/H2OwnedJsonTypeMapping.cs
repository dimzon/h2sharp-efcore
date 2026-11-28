using System.Data.Common;
using System.Text;
using System.Text.Json;
using H2Sharp.Data;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     Supports the standard EF JSON support, which relies on owned entity modeling.
///     See <see cref="H2JsonTypeMapping" /> for the older H2-specific support, which allows mapping json
///     to text, to e.g.
///     <see cref="JsonElement" /> (weakly-typed mapping) or to arbitrary POCOs (but without them being modeled).
/// </summary>
public class H2OwnedJsonTypeMapping : JsonTypeMapping
{
    // The engine keeps a JSON value as normalized UTF-8 bytes, and H2DataReader.GetStream hands
    // them out as a MemoryStream over those bytes. No text decoding/re-encoding is needed.
    private static readonly MethodInfo GetStreamMethod
        = ExpressionMemberHelper.Method<Func<DbDataReader, int, System.IO.Stream>>(
            (reader, index) => reader.GetStream(index));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2OwnedJsonTypeMapping(string storeType)
        : base(storeType, typeof(JsonElement), dbType: null)
    {
        if (storeType != "json")
        {
            throw new ArgumentException("Only the json type is supported", nameof(storeType));
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override MethodInfo GetDataReaderMethod()
        => GetStreamMethod;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Expression CustomizeDataReaderExpression(Expression expression)
        => expression;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2OwnedJsonTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
    {
        if (parameter is not H2Parameter)
        {
            throw new InvalidOperationException(
                $"H2-specific type mapping {nameof(H2OwnedJsonTypeMapping)} being used with non-H2 parameter type {parameter.GetType().Name}");
        }

        base.ConfigureParameter(parameter);

        // EF hands the serialized document to the provider as a string. A plain string bound to
        // a JSON column becomes a JSON string literal, so wrap it: the engine then parses it as a
        // document through H2JsonParameterValue.
        if (parameter.Value is string json && json.Length > 0)
        {
            parameter.Value = new JsonTextParameterValue(json);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual string EscapeSqlLiteral(string literal)
        => literal.Replace("'", "''");

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
        => $"'{EscapeSqlLiteral(JsonSerializer.Serialize(value))}'";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2OwnedJsonTypeMapping(parameters);

    private sealed class JsonTextParameterValue(string text) : H2JsonParameterValue
    {
        private byte[]? bytes;

        public override byte[] GetUtf8Bytes()
            => bytes ??= Encoding.UTF8.GetBytes(text);
    }
}
