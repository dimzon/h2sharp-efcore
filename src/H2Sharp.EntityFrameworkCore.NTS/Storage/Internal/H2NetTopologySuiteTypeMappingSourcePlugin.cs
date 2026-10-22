using System.Diagnostics.CodeAnalysis;
using System.Globalization;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2NetTopologySuiteTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    private static bool TryGetClrType(string subtypeName, [NotNullWhen(true)] out Type? clrType)
    {
        clrType = subtypeName switch
        {
            "POINT" => typeof(Point),
            "LINESTRING" => typeof(LineString),
            "POLYGON" => typeof(Polygon),
            "MULTIPOINT" => typeof(MultiPoint),
            "MULTILINESTRING" => typeof(MultiLineString),
            "MULTIPOLYGON" => typeof(MultiPolygon),
            "GEOMETRYCOLLECTION" => typeof(GeometryCollection),
            "GEOMETRY" => typeof(Geometry),
            _ => null
        };

        return clrType is not null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2NetTopologySuiteTypeMappingSourcePlugin()
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        // TODO: Array
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;

        if (clrType is not null)
        {
            if (!clrType.IsAssignableTo(typeof(Geometry)))
            {
                return null;
            }

            // TODO: if store type is null, consider setting it based on the CLR type, i.e. create GEOMETRY(Point) instead of Geometry when
            // the CLR property is NTS Point.
        }

        if (storeTypeName is not null)
        {
            if (!TryParseStoreTypeName(storeTypeName, out _, out var parsedSubtype, out _, out _))
            {
                return null;
            }

            clrType ??= parsedSubtype;
        }

        storeTypeName ??= "geometry";

        Check.DebugAssert(clrType is not null, "clrType is not null");

        var typeMapping = (RelationalTypeMapping)Activator.CreateInstance(
            typeof(H2GeometryTypeMapping<>).MakeGenericType(clrType), storeTypeName)!;

        // TODO: for geometry collection support (and why the following is commented out), see #2850.

        // // TODO: Also restrict the element type mapping based on the user-specified store type?
        // var elementType = clrType == typeof(MultiPoint)
        //     ? typeof(Point)
        //     : clrType == typeof(MultiLineString)
        //         ? typeof(LineString)
        //         : clrType == typeof(MultiPolygon)
        //             ? typeof(Polygon)
        //             : clrType == typeof(GeometryCollection)
        //                 ? typeof(Geometry)
        //                 : null;
        //
        // if (elementType is not null)
        // {
        //     var elementTypeMapping = FindMapping(new() { ClrType = elementType })!;
        //
        //     typeMapping = typeMapping.Clone(elementMapping: elementTypeMapping);
        // }

        return typeMapping;
    }

    /// <summary>
    ///     Parses an H2 GEOMETRY type, including its shape, dimension system and optional SRID.
    /// </summary>
    public static bool TryParseStoreTypeName(
        string storeTypeName,
        out string subtypeName,
        out Type? clrType,
        out int srid,
        out Ordinates ordinates)
    {
        storeTypeName = storeTypeName.Trim();
        subtypeName = storeTypeName;
        clrType = typeof(Geometry);
        srid = -1;
        ordinates = Ordinates.AllOrdinates;

        var openParen = storeTypeName.IndexOf("(", StringComparison.Ordinal);

        var baseType = openParen > 0 ? storeTypeName.Substring(0, openParen).Trim() : storeTypeName;

        if (!baseType.Equals("GEOMETRY", StringComparison.OrdinalIgnoreCase))
        {
            // H2 2.5 has one geometry type and no geography.
            return false;
        }

        if (openParen == -1)
        {
            return true;
        }

        var closeParen = storeTypeName.IndexOf(")", openParen + 1, StringComparison.Ordinal);
        if (closeParen != storeTypeName.Length - 1)
        {
            return false;
        }

        var comma = storeTypeName.IndexOf(",", openParen + 1, StringComparison.Ordinal);
        if (comma == -1)
        {
            subtypeName = storeTypeName.Substring(openParen + 1, closeParen - openParen - 1).Trim();
        }
        else
        {
            subtypeName = storeTypeName.Substring(openParen + 1, comma - openParen - 1).Trim();
            if (!int.TryParse(storeTypeName.Substring(comma + 1, closeParen - comma - 1).Trim(),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out srid))
            {
                return false;
            }
        }

        subtypeName = subtypeName.ToUpperInvariant().Replace(" ", "", StringComparison.Ordinal);

        // We have geometry(subtype, srid), parse the subtype (POINT, POINTZ, POINTM, POINTZM...)

        if (TryGetClrType(subtypeName, out clrType))
        {
            return true;
        }

        if (subtypeName.EndsWith("ZM", StringComparison.Ordinal) && TryGetClrType(subtypeName[..^2], out clrType))
        {
            ordinates = Ordinates.XYZM;
            return true;
        }

        if (subtypeName.EndsWith("M", StringComparison.Ordinal) && TryGetClrType(subtypeName[..^1], out clrType))
        {
            ordinates = Ordinates.XYM;
            return true;
        }

        if (subtypeName.EndsWith("Z", StringComparison.Ordinal) && TryGetClrType(subtypeName[..^1], out clrType))
        {
            ordinates = Ordinates.XYZ;
            return true;
        }

        return false;
    }
}
