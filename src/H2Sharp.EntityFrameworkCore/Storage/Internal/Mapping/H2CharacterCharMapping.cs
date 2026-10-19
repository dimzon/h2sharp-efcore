using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     Type mapping for the H2 CHARACTER data type, which handles both CLR strings and chars.
/// </summary>
/// <remarks>
///     See docs/DATATYPES.md
/// </remarks>
public class H2CharacterCharTypeMapping : CharTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static new H2CharacterCharTypeMapping Default { get; } = new("character");

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2CharacterCharTypeMapping(string storeType)
        : this(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(typeof(char), jsonValueReaderWriter: JsonCharReaderWriter.Instance),
                storeType,
                StoreTypePostfix.Size,
                System.Data.DbType.StringFixedLength,
                unicode: false,
                fixedLength: true))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2CharacterCharTypeMapping(RelationalTypeMappingParameters parameters)
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
        => new H2CharacterCharTypeMapping(parameters);

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
}
