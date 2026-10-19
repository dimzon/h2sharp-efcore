using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     The base class for mapping H2-specific types.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this class carried a provider-specific type enum and wrote it onto every parameter,
///         because the Npgsql wire protocol needs it. H2Sharp's ADO layer has no such enum: a parameter
///         is described by <see cref="DbParameter.DbType" /> and the engine infers the H2 type from the
///         value. So the mapping's job here is the store type name and the literal rendering, and nothing
///         else is pushed down to the parameter.
///     </para>
/// </remarks>
public abstract class H2TypeMapping : RelationalTypeMapping
{
    // ReSharper disable once PublicConstructorInAbstractClass
    /// <summary>
    ///     Constructs an instance of the <see cref="H2TypeMapping" /> class.
    /// </summary>
    /// <param name="storeType">The database type to map.</param>
    /// <param name="clrType">The CLR type to map.</param>
    /// <param name="jsonValueReaderWriter">Handles reading and writing JSON values for instances of the mapped type.</param>
    public H2TypeMapping(string storeType, Type clrType, JsonValueReaderWriter? jsonValueReaderWriter = null)
        : base(storeType, clrType, jsonValueReaderWriter: jsonValueReaderWriter)
    {
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="H2TypeMapping" /> class for a type the database does not
    ///     hold natively, and which therefore reaches it through a conversion.
    /// </summary>
    /// <param name="storeType">The database type to map.</param>
    /// <param name="clrType">The CLR type the model holds.</param>
    /// <param name="converter">The conversion between the model type and the type the engine understands.</param>
    /// <param name="jsonValueReaderWriter">Handles reading and writing JSON values for instances of the mapped type.</param>
    /// <remarks>
    ///     The H2 engine's value mapper knows the BCL types only. A type it has never heard of -- NodaTime's
    ///     LocalDate, for instance -- reaches the database as the BCL type the conversion produces, and the
    ///     store type says which H2 column it lands in.
    /// </remarks>
    public H2TypeMapping(
        string storeType,
        Type clrType,
        ValueConverter? converter,
        JsonValueReaderWriter? jsonValueReaderWriter = null)
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(clrType, converter, jsonValueReaderWriter: jsonValueReaderWriter),
                storeType))
    {
    }

    /// <summary>
    ///     Constructs an instance of the <see cref="H2TypeMapping" /> class.
    /// </summary>
    /// <param name="parameters">The parameters for this mapping.</param>
    protected H2TypeMapping(RelationalTypeMappingParameters parameters)
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
                $"H2-specific type mapping {GetType().Name} being used with non-H2 parameter type {parameter.GetType().Name}");
        }

        base.ConfigureParameter(parameter);
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
