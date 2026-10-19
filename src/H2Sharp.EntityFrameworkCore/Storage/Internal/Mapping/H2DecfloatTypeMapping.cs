using System.Globalization;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     Maps <see cref="decimal" /> to H2's DECFLOAT.
/// </summary>
/// <remarks>
///     <para>
///         DECFLOAT is H2's arbitrary-precision decimal with a decimal exponent, defined by the General
///         Decimal Arithmetic specification. It is not NUMERIC: NUMERIC has fixed scale and precision and
///         is what <see cref="H2DecimalTypeMapping" /> maps. DECFLOAT is chosen here because it can hold
///         every <see cref="decimal" /> without the caller having to pick a precision and scale up front,
///         which is the same reason the mapping upstream picked NUMERIC without a precision.
///     </para>
/// </remarks>
public class H2DecfloatTypeMapping : H2TypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static H2DecfloatTypeMapping Default { get; } = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2DecfloatTypeMapping(Type? clrType = null)
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(clrType ?? typeof(decimal)),
            "decfloat"))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2DecfloatTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2DecfloatTypeMapping(parameters);

    /// <inheritdoc />
    protected override string GenerateNonNullSqlLiteral(object value)
        => Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
}
