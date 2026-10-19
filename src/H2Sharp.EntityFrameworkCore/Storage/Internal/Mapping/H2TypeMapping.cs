using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     The base class for mapping H2-specific types. It configures parameters with the
///     <see cref="H2DbType" /> provider-specific type enum.
/// </summary>
public abstract class H2TypeMapping : RelationalTypeMapping, IH2TypeMapping
{
    /// <inheritdoc />
    public virtual H2DbType H2DbType { get; }

    // ReSharper disable once PublicConstructorInAbstractClass
    /// <summary>
    ///     Constructs an instance of the <see cref="H2TypeMapping" /> class.
    /// </summary>
    /// <param name="storeType">The database type to map.</param>
    /// <param name="clrType">The CLR type to map.</param>
    /// <param name="npgsqlDbType">The database type used by H2.</param>
    /// <param name="jsonValueReaderWriter">Handles reading and writing JSON values for instances of the mapped type.</param>
    public H2TypeMapping(string storeType, Type clrType, H2DbType npgsqlDbType, JsonValueReaderWriter? jsonValueReaderWriter = null)
        : base(storeType, clrType, jsonValueReaderWriter: jsonValueReaderWriter)
    {
        H2DbType = npgsqlDbType;
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="H2TypeMapping" /> class.
    /// </summary>
    /// <param name="parameters">The parameters for this mapping.</param>
    /// <param name="npgsqlDbType">The database type of the range subtype.</param>
    protected H2TypeMapping(RelationalTypeMappingParameters parameters, H2DbType npgsqlDbType)
        : base(parameters)
    {
        H2DbType = npgsqlDbType;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
    {
        if (parameter is not H2Parameter npgsqlParameter)
        {
            throw new InvalidOperationException(
                $"H2-specific type mapping {GetType().Name} being used with non-H2 parameter type {parameter.GetType().Name}");
        }

        base.ConfigureParameter(parameter);
        npgsqlParameter.H2DbType = H2DbType;
    }

    /// <summary>
    ///     Generates the SQL representation of a literal value meant to be embedded in another literal value, e.g. in a range.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>
    ///     The generated string.
    /// </returns>
    public virtual string GenerateEmbeddedSqlLiteral(object? value)
    {
        value = ConvertUnderlyingEnumValueToEnum(value);

        if (Converter != null)
        {
            value = Converter.ConvertToProvider(value);
        }

        return GenerateEmbeddedProviderValueSqlLiteral(value);
    }

    /// <summary>
    ///     Generates the SQL representation of a literal value without conversion, meant to be embedded in another literal value,
    ///     e.g. in a range.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>
    ///     The generated string.
    /// </returns>
    public virtual string GenerateEmbeddedProviderValueSqlLiteral(object? value)
        => value == null
            ? "NULL"
            : GenerateEmbeddedNonNullSqlLiteral(value);

    /// <summary>
    ///     Generates the SQL representation of a non-null literal value, meant to be embedded in another literal value, e.g. in a range.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>
    ///     The generated string.
    /// </returns>
    protected virtual string GenerateEmbeddedNonNullSqlLiteral(object value)
        => GenerateNonNullSqlLiteral(value);

    // Copied from RelationalTypeMapping
    private object? ConvertUnderlyingEnumValueToEnum(object? value)
        => value?.GetType().IsInteger() == true && ClrType.UnwrapNullableType().IsEnum
            ? Enum.ToObject(ClrType.UnwrapNullableType(), value)
            : value;
}
