using System.Globalization;
using System.Text;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         H2 has a single GEOMETRY type with a subtype per shape (POINT, LINESTRING, POLYGON, …),
///         where PostgreSQL has one column type per shape (point, box, line, lseg, path, polygon,
///         circle). The mapping therefore keeps the shape in the store type name rather than in a
///         separate mapping class: the shape is what the column is actually declared as, and EF needs
///         it to round-trip a schema.
///     </para>
/// </remarks>
public class H2GeometryTypeMapping : H2TypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static H2GeometryTypeMapping Default { get; } = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2GeometryTypeMapping(string storeType = "geometry")
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(typeof(string)),
            storeType))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2GeometryTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2GeometryTypeMapping(parameters);

    /// <inheritdoc />
    protected override string GenerateNonNullSqlLiteral(object value)
    {
        Check.NotNull(value, nameof(value));

        // H2 has no escape-string form for geometry; the well-known text form is a single quoted string.
        var builder = new StringBuilder(value.ToString()!.Length + 2);
        builder.Append('\'').Append(value).Append('\'');

        return builder.ToString();
    }
}
