using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2TypeMappingSource : RelationalTypeMappingSource
{
#if DEBUG
    internal static bool LegacyTimestampBehavior;

    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    internal static bool DisableDateTimeInfinityConversions;
#else
    internal static readonly bool LegacyTimestampBehavior;
    internal static readonly bool DisableDateTimeInfinityConversions;
#endif

    static H2TypeMappingSource()
    {
        LegacyTimestampBehavior = AppContext.TryGetSwitch("H2.EnableLegacyTimestampBehavior", out var enabled) && enabled;
        DisableDateTimeInfinityConversions = AppContext.TryGetSwitch("H2.DisableDateTimeInfinityConversions", out enabled) && enabled;
    }

    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual ConcurrentDictionary<string, RelationalTypeMapping[]> StoreTypeMappings { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual ConcurrentDictionary<Type, RelationalTypeMapping> ClrTypeMappings { get; }

    private readonly IReadOnlyList<EnumDefinition> _enumDefinitions;

    #region Mappings

    // Numeric types
    private readonly H2FloatTypeMapping _float4 = H2FloatTypeMapping.Default;
    private readonly H2DoubleTypeMapping _float8 = H2DoubleTypeMapping.Default;
    private readonly H2DecimalTypeMapping _numeric = H2DecimalTypeMapping.Default;
    private readonly H2BigIntegerTypeMapping _bigInteger = H2BigIntegerTypeMapping.Default;
    private readonly H2DecimalTypeMapping _numericAsFloat = new(typeof(float));
    private readonly H2DecimalTypeMapping _numericAsDouble = new(typeof(double));
    private readonly H2DecfloatTypeMapping _decfloat = H2DecfloatTypeMapping.Default;
    private readonly GuidTypeMapping _uuid = new("uuid", DbType.Guid);
    private readonly SByteTypeMapping _int1 = new("tinyint", DbType.SByte);
    private readonly ShortTypeMapping _int2 = new("smallint", DbType.Int16);
    private readonly ByteTypeMapping _int2Byte = new("smallint", DbType.Byte);
    private readonly IntTypeMapping _int4 = new("integer", DbType.Int32);
    private readonly LongTypeMapping _int8 = new("bigint", DbType.Int64);

    // Character types
    //
    // A string is a CHARACTER VARYING in H2 2.5: "for variable-length strings use CHARACTER
    // VARYING". Without a length it takes the maximum, so a string of any size fits.
    //
    // CHARACTER LARGE OBJECT is a different thing: H2 keeps such values out of memory and
    // streams them, and it is meant for documents and texts of arbitrary size. PostgreSQL's text
    // is such a value, which is why the text of PostgreSQL became a clob here; but the string of
    // a program is a string, and H2's own type for one is a varchar.
    private readonly StringTypeMapping _text = new("varchar", DbType.String);
    private readonly H2StringTypeMapping _varchar = new("varchar");
    private readonly H2CharacterStringTypeMapping _char = new("char");
    private readonly H2CharacterCharTypeMapping _singleChar = new("character(1)");

    // JSON mappings - EF owned entity support
    private readonly H2OwnedJsonTypeMapping _jsonOwned = new("json");

    // JSON mappings - older string/weakly-typed support
    private readonly H2JsonTypeMapping _jsonString = new("json", typeof(string));
    private readonly H2JsonTypeMapping _jsonDocument = new("json", typeof(JsonDocument));
    private readonly H2JsonTypeMapping _jsonElement = new("json", typeof(JsonElement));

    // Date/Time types
    private readonly H2DateTimeDateTypeMapping _dateDateTime = H2DateTimeDateTypeMapping.Default;
    private readonly H2TimestampTypeMapping _timestamp = H2TimestampTypeMapping.Default;
    private readonly H2TimestampTzTypeMapping _timestamptz = H2TimestampTzTypeMapping.Default;
    private readonly H2TimestampTzTypeMapping _timestamptzDto = new(typeof(DateTimeOffset));
    private readonly H2IntervalTypeMapping _interval = H2IntervalTypeMapping.Default;
    private readonly H2TimeTypeMapping _timeTimeSpan = new(typeof(TimeSpan));
    private readonly H2TimeTzTypeMapping _timetz = H2TimeTzTypeMapping.Default;

    private readonly H2DateOnlyTypeMapping _dateDateOnly = H2DateOnlyTypeMapping.Default;
    private readonly H2TimeTypeMapping _timeTimeOnly = H2TimeTypeMapping.Default;

    // Binary types
    private readonly H2ByteArrayTypeMapping _varBinary = H2ByteArrayTypeMapping.Default;
    private readonly H2BlobTypeMapping _blob = H2BlobTypeMapping.Default;

    // BOOLEAN is a distinct H2 type, not an alias for an integer: it does not accept 0/1, and
    // arithmetic on it fails. That is the one place where reusing a numeric mapping would be wrong.
    private readonly H2BoolTypeMapping _bool = H2BoolTypeMapping.Default;

    // Geometry. H2 stores points as GEOMETRY with a POINT subtype, unlike PostgreSQL's bare 'point'.
    private readonly H2GeometryTypeMapping _geometry = H2GeometryTypeMapping.Default;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    // Special stuff
    // ReSharper disable once InconsistentNaming
    public readonly H2EStringTypeMapping EStringTypeMapping = H2EStringTypeMapping.Default;

    #endregion Mappings

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2TypeMappingSource(
        TypeMappingSourceDependencies dependencies,
        RelationalTypeMappingSourceDependencies relationalDependencies,
        ISqlGenerationHelper sqlGenerationHelper,
        IH2SingletonOptions options)
        : base(dependencies, relationalDependencies)
    {
        _sqlGenerationHelper = Check.NotNull(sqlGenerationHelper, nameof(sqlGenerationHelper));

// ReSharper disable CoVariantArrayConversion
        // H2 2.5 store types. Both the canonical spelling and the documented aliases are registered,
        // so that scaffolding a database created with either spelling still finds its mapping.
        // See docs/DATATYPES.md.
        var storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "tinyint", [_int1] },
            { "smallint", [_int2, _int2Byte] },
            { "integer", [_int4] },
            { "int", [_int4] },
            { "bigint", [_int8] },
            { "real", [_float4] },
            { "double precision", [_float8] },
            { "numeric", [_numeric, _bigInteger, _numericAsFloat, _numericAsDouble] },
            { "decimal", [_numeric, _bigInteger, _numericAsFloat, _numericAsDouble] },
            { "decfloat", [_decfloat] },

            { "character varying", [_varchar] },
            { "varchar", [_varchar] },
            { "character large object", [_text] },
            { "clob", [_text] },
            // TEXT is one of the aliases of CHARACTER VARYING in H2 2.5 (DataType.java), and a
            // database created with that spelling has to be scaffolded back to the same mapping.
            { "text", [_text] },
            // See FindBaseMapping below for special treatment of 'character'

            { "boolean", [_bool] },

            { "timestamp", [_timestamp] },
            { "timestamp with time zone", [_timestamptz, _timestamptzDto] },
            { "time", [_timeTimeOnly, _timeTimeSpan] },
            { "time with time zone", [_timetz] },
            { "date", [_dateDateOnly, _dateDateTime] },
            { "interval", [_interval] },

            { "binary varying", [_varBinary] },
            { "varbinary", [_varBinary] },
            { "binary large object", [_blob] },
            { "blob", [_blob] },

            { "uuid", [_uuid] },
            { "json", [_jsonString, _jsonDocument, _jsonElement] },
            { "geometry", [_geometry] }
        };
// ReSharper restore CoVariantArrayConversion

        // H2 spells the time-zone-qualified types in full; keep the short PostgreSQL-style names as
        // aliases so a store type written either way resolves to the same mapping.
        storeTypeMappings["timestamptz"] = storeTypeMappings["timestamp with time zone"];
        storeTypeMappings["timetz"] = storeTypeMappings["time with time zone"];

        var clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
        {
            { typeof(bool), _bool },
            { typeof(Guid), _uuid },
            { typeof(sbyte), _int1 },
            { typeof(byte), _int2Byte },
            { typeof(short), _int2 },
            { typeof(int), _int4 },
            { typeof(long), _int8 },
            { typeof(float), _float4 },
            { typeof(double), _float8 },
            { typeof(decimal), _numeric },
            { typeof(BigInteger), _bigInteger },
            { typeof(string), _text },
            { typeof(JsonDocument), _jsonDocument },
            { typeof(JsonElement), _jsonElement },
            { typeof(char), _singleChar },
            { typeof(DateTime), LegacyTimestampBehavior ? _timestamptz : _timestamp },
            { typeof(DateOnly), _dateDateOnly },
            { typeof(TimeOnly), _timeTimeOnly },
            { typeof(TimeSpan), _interval },
            { typeof(DateTimeOffset), _timestamptzDto },
            { typeof(byte[]), _varBinary }
        };

        StoreTypeMappings = new ConcurrentDictionary<string, RelationalTypeMapping[]>(storeTypeMappings, StringComparer.OrdinalIgnoreCase);
        ClrTypeMappings = new ConcurrentDictionary<Type, RelationalTypeMapping>(clrTypeMappings);

        _enumDefinitions = options.EnumDefinitions;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
        // First, try any plugins, allowing them to override built-in mappings (e.g. NodaTime)
        => base.FindMapping(mappingInfo)
            ?? FindBaseMapping(mappingInfo)?.Clone(mappingInfo)
            ?? FindEnumMapping(mappingInfo)
            ?? FindRowValueMapping(mappingInfo)?.Clone(mappingInfo);
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual RelationalTypeMapping? FindBaseMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;
        var storeTypeNameBase = mappingInfo.StoreTypeNameBase;

        if (storeTypeName is not null)
        {
            if (StoreTypeMappings.TryGetValue(storeTypeName, out var mappings))
            {
                // We found the user-specified store type. No CLR type was provided - we're probably
                // scaffolding from an existing database, take the first mapping as the default.
                if (clrType is null)
                {
                    return mappings[0];
                }

                // A CLR type was provided - look for a mapping between the store and CLR types. If not found, fail
                // immediately.
                foreach (var m in mappings)
                {
                    if (m.ClrType == clrType)
                    {
                        return m;
                    }
                }

                // Map arbitrary user POCOs to JSON
                if (storeTypeName is "json")
                {
                    return new H2JsonTypeMapping(storeTypeName, clrType);
                }

                return null;
            }

            if (StoreTypeMappings.TryGetValue(storeTypeNameBase!, out mappings))
            {
                if (clrType is null)
                {
                    return mappings[0];
                }

                foreach (var m in mappings)
                {
                    if (m.ClrType == clrType)
                    {
                        return m;
                    }
                }

                return null;
            }

            // 'character' is special: 'character' (no size) and 'character(1)' map to a single char, whereas 'character(n)' maps
            // to a string
            if (storeTypeNameBase is "character" or "char")
            {
                if (mappingInfo.Size is null or 1 && clrType is null || clrType == typeof(char))
                {
                    return _singleChar.Clone(mappingInfo);
                }

                if (clrType is null || clrType == typeof(string))
                {
                    return _char.Clone(mappingInfo);
                }
            }

            if ((storeTypeName.EndsWith("[]", StringComparison.Ordinal)
                    || storeTypeName.EndsWith(" ARRAY", StringComparison.Ordinal)
                    || storeTypeName is "int4multirange" or "int8multirange" or "nummultirange" or "datemultirange" or "tsmultirange"
                        or "tstzmultirange")
                && FindCollectionMapping(mappingInfo, mappingInfo.ClrType, providerType: null, elementMapping: null) is
                    RelationalTypeMapping collectionMapping)
            {
                return collectionMapping;
            }

            // A store type name was provided, but is unknown. This could be a domain (alias) type, in which case
            // we proceed with a CLR type lookup (if the type doesn't exist at all the failure will come later).
        }

        if (clrType is not null)
        {
            if (ClrTypeMappings.TryGetValue(clrType, out var mapping))
            {
                // Handle types with the size facet (string, bitarray)
                if (mappingInfo.Size > 0)
                {
                    if (clrType == typeof(string))
                    {
                        mapping = mappingInfo.IsFixedLength ?? false ? _char : _varchar;

                        // See #342 for when size > 10485760
                        return mappingInfo.Size <= 10485760
                            ? mapping.WithStoreTypeAndSize($"{mapping.StoreType}({mappingInfo.Size})", mappingInfo.Size)
                            : _text;
                    }
                    // H2 has no BIT/VARBIT type, so BitArray has no store type to map to.
                }

                return mapping;
            }

            if (clrType == typeof(byte[]) && mappingInfo.ElementTypeMapping is null)
            {
                if (storeTypeName == "smallint[]")
                {
                    // PostgreSQL has no tinyint (single-byte) type, but we allow mapping CLR byte to PG smallint (2-bytes).
                    // The same applies to arrays - as always - so byte[] should be mappable to smallint[].
                    // However, byte[] also has a base mapping to VARBINARY, which is the default. So when the user explicitly specified
                    // mapping to smallint[], we don't return that to allow the array mapping to work.
                    // TODO: This is a workaround; RelationalTypeMappingSource first attempts to find a value converter before trying
                    // to find a collection. We should reverse the order and call FindCollectionMapping before attempting to find a
                    // value converter.
                    // TODO: Make sure the providerType should be null
                    return FindCollectionMapping(mappingInfo, typeof(byte[]), providerType: null, elementMapping: null);
                    // return null;
                }

                return _varBinary;
            }
            // H2 has no xid/xid8 system column types, so a row-version property gets no special
            // mapping: byte[] falls through to the VARBINARY mapping above.
        }

        return null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping? FindCollectionMapping(
        RelationalTypeMappingInfo info,
        // Note that modelType is nullable (in the relational base signature it isn't) because of array scaffolding, i.e. we call
        // FindCollectionMapping from our own FindMapping, where the clrType is null when scaffolding.
        Type? modelType,
        Type? providerType,
        CoreTypeMapping? elementMapping)
    {
        if (elementMapping is not null and not RelationalTypeMapping)
        {
            return null;
        }

        Type concreteCollectionType;
        Type? elementType = null;

        if (modelType is not null)
        {
            // We do GetElementType for multidimensional arrays - these don't implement generic IEnumerable<>
            elementType = modelType.TryGetElementType(typeof(IEnumerable<>)) ?? modelType.GetElementType();

            // E.g. Newtonsoft.Json's JToken is enumerable over itself, exclude that scenario to avoid stack overflow.
            if (elementType is null || elementType == modelType || modelType.GetGenericTypeImplementations(typeof(IDictionary<,>)).Any())
            {
                return null;
            }
        }

        var storeType = info.StoreTypeName;
        if (storeType is null)
        {
            if (modelType is null)
            {
                return null;
            }

            // If no mapping was found for the element CLR type, there's no mapping for the array.
            // Also, arrays of arrays aren't supported (as opposed to multidimensional arrays) by PostgreSQL
            Check.DebugAssert(elementType is not null, "elementClrType is null");

            var relationalElementMapping = elementMapping as RelationalTypeMapping ?? FindMapping(elementType);
            if (relationalElementMapping is not { ElementTypeMapping: null })
            {
                return null;
            }

            // Multiranges are gone with the range types themselves: H2 has no range types, so there is
            // no multirange to build an array over. Everything below maps as a plain H2 array.

            // Not a multirange - map as a PG array type
            concreteCollectionType = FindTypeToInstantiate(modelType, elementType);

            return (H2ArrayTypeMapping)Activator.CreateInstance(
                typeof(H2ArrayTypeMapping<,,>).MakeGenericType(modelType, concreteCollectionType, elementType),
                relationalElementMapping)!;
        }

        if (storeType.EndsWith("[]", StringComparison.Ordinal) || storeType.EndsWith(" ARRAY", StringComparison.Ordinal))
        {            // We have an array store type (either because we're reverse engineering or the user explicitly specified it).
            // H2 2.5 writes the type of an array as the type of its elements and the word ARRAY after
            // it ("integer ARRAY"); the brackets of PostgreSQL ("integer[]") it does not know, but a
            // store type written that way is still read, so that the mapping of an array found in a
            // database is the same as the one the model asks for.
            var elementStoreType = storeType.EndsWith(" ARRAY", StringComparison.Ordinal)
                ? storeType.Substring(0, storeType.Length - " ARRAY".Length)
                : storeType.Substring(0, storeType.Length - 2);

            // Note that we ignore the elementMapping argument here (but not in the CLR type-only path above).
            // This is because the user-provided storeType for the array should take precedence over the element type mapping that gets
            // calculated purely based on the element's CLR type in base.FindMappingWithConversion.
            var relationalElementMapping = elementMapping as RelationalTypeMapping
                ?? (elementType is null
                    ? FindMapping(elementStoreType)
                    : FindMapping(elementType, elementStoreType));
            if (relationalElementMapping is not { ElementTypeMapping: null })
            {
                return null;
            }

            // If no mapping was found for the element, there's no mapping for the array.
            // Also, arrays of arrays aren't supported (as opposed to multidimensional arrays) by PostgreSQL
            if (relationalElementMapping is not null and not H2ArrayTypeMapping)
            {
                if (modelType is null)
                {
                    // There's no model type - we're scaffolding purely from the store type.
                    elementType = relationalElementMapping.ClrType;
                    modelType = concreteCollectionType = typeof(List<>).MakeGenericType(elementType);
                }
                else
                {
                    concreteCollectionType = FindTypeToInstantiate(modelType, elementType!);
                    Check.DebugAssert(elementType is not null, "elementType is null");
                }

                return (H2ArrayTypeMapping)Activator.CreateInstance(
                    typeof(H2ArrayTypeMapping<,,>).MakeGenericType(modelType, concreteCollectionType, elementType),
                    storeType, relationalElementMapping)!;
            }
        }
        // Multirange store types are gone with the range types themselves.

        return null;


        static Type FindTypeToInstantiate(Type collectionType, Type elementType)
        {
            if (collectionType.IsArray)
            {
                return collectionType;
            }

            var listOfT = typeof(List<>).MakeGenericType(elementType);

            if (collectionType.IsAssignableFrom(listOfT))
            {
                if (!collectionType.IsAbstract)
                {
                    var constructor = collectionType.GetDeclaredConstructor(null);
                    if (constructor?.IsPublic == true)
                    {
                        return collectionType;
                    }
                }

                return listOfT;
            }

            return collectionType;
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual RelationalTypeMapping? FindRowValueMapping(in RelationalTypeMappingInfo mappingInfo)
        => mappingInfo.ClrType is { } clrType
            && clrType.IsAssignableTo(typeof(ITuple))
                ? new H2RowValueTypeMapping(clrType)
                : null;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual RelationalTypeMapping? FindEnumMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var storeType = mappingInfo.StoreTypeName;
        var clrType = mappingInfo.ClrType;
        string? schema;
        string name;

        if (clrType is not null and not { IsEnum: true, IsClass: false })
        {
            return null;
        }

        // Try to find an enum definition (defined by the user on their context options), based on the
        // incoming MappingInfo's StoreType or ClrType
        EnumDefinition? enumDefinition;
        if (storeType is null)
        {
            enumDefinition = _enumDefinitions.SingleOrDefault(m => m.ClrType == clrType);

            if (enumDefinition is null)
            {
                return null;
            }

            (name, schema) = (enumDefinition.StoreTypeName, enumDefinition.StoreTypeSchema);
        }
        else
        {
            // If the user is specifying the store type manually, they are not expected to have quotes in the name (e.g. because of upper-
            // case characters).
            // However, if we infer an enum array type mapping from an element (e.g. someEnums.Contains(b.SomeEnumColumn)), we get the
            // element's store type - which for enums is quoted - and add []; so we get e.g. "MyEnum"[]. So we need to support quoted
            // names here, by parsing the name and stripping the quotes.
            ParseStoreTypeName(storeType, out name, out schema, out var size, out var precision, out var scale);

            enumDefinition = schema is null
                ? _enumDefinitions.SingleOrDefault(m => m.StoreTypeName == name)
                : _enumDefinitions.SingleOrDefault(m => m.StoreTypeName == name && m.StoreTypeSchema == schema);

            if (enumDefinition is null)
            {
                return null;
            }
        }

        // We now have an enum definition from the context options.

        // We need the following store type names:
        // 1. The quoted type name is used in migrations, where quoting is needed
        // 2. The unquoted type name is set on H2Parameter.DataTypeName
        //    (though see https://github.com/npgsql/npgsql/issues/5710).
        return new H2EnumTypeMapping(
            _sqlGenerationHelper.DelimitIdentifier(name, schema),
            schema is null ? name : schema + "." + name,
            enumDefinition.ClrType,
            enumDefinition.Labels);
    }
    /// <summary>
    ///     Finds the mapping for a container given its CLR type and its containee's type mapping; this is used when inferring type mappings
    ///     for arrays and ranges/multiranges.
    /// </summary>
    public virtual RelationalTypeMapping? FindContainerMapping(
        Type containerClrType,
        RelationalTypeMapping containeeTypeMapping,
        IModel model)
    {
        // Ranges and multiranges have no H2 equivalent, so the only container left is an array, which
        // the standard EF lookup below already handles.

        // Then, try to find the mapping with the containee mapping as the element type mapping.
        // This is the standard EF lookup mechanism, and takes care of regular arrays and multiranges, which are supported as full primitive
        // collections.
        if (FindMapping(containerClrType, model, containeeTypeMapping) is RelationalTypeMapping containerMapping)
        {
            return containerMapping;
        }

        return null;
    }

    private static bool NameBasesUsesPrecision(ReadOnlySpan<char> span)
        => span.ToString() switch
        {
            "decimal" => true,
            "dec" => true,
            "numeric" => true,
            "timestamp" => true,
            "timestamptz" => true,
            "time" => true,
            "interval" => true,
            _ => false
        };

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    // We override to support parsing array store names (e.g. varchar(32)[]), timestamp(5) with time zone, etc.
    protected override string? ParseStoreTypeName(
        string? storeTypeName,
        ref bool? unicode,
        ref int? size,
        ref int? precision,
        ref int? scale)
    {
        // TODO: Reimplement over ParseStoreTypeName below

        if (storeTypeName is null)
        {
            return null;
        }

        var s = storeTypeName.AsSpan().Trim();

        // If this is an array store type, any facets (size, precision...) apply to the element and not to the array (e.g. varchar(32)[]
        // is an array mapping with Size=null over an element mapping of varchar with Size=32).
        if (s.EndsWith("[]", StringComparison.Ordinal))
        {
            return storeTypeName;
        }

        var openParen = s.IndexOf("(", StringComparison.Ordinal);
        if (openParen == -1)
        {
            return storeTypeName;
        }

        var preParens = s[..openParen].Trim();
        s = s.Slice(openParen + 1);
        var closeParen = s.IndexOf(")", StringComparison.Ordinal);
        if (closeParen == -1)
        {
            return storeTypeName;
        }

        var inParens = s[..closeParen].Trim();
        // There may be stuff after the closing parentheses (e.g. timestamp(3) with time zone)
        var postParens = s.Slice(closeParen + 1);

        switch (s.IndexOf(",", StringComparison.Ordinal))
        {
            // No comma inside the parentheses, parse the value either as size or precision
            case -1:
                if (!int.TryParse(inParens, out var p))
                {
                    return storeTypeName;
                }

                if (NameBasesUsesPrecision(preParens))
                {
                    precision = p;
                    scale = 0;
                }
                else
                {
                    size = p;
                }

                break;

            case var comma:
                if (int.TryParse(s[..comma].Trim(), out var parsedPrecision))
                {
                    precision = parsedPrecision;
                }
                else
                {
                    return storeTypeName;
                }

                if (int.TryParse(s[(comma + 1)..closeParen].Trim(), out var parsedScale))
                {
                    scale = parsedScale;
                }
                else
                {
                    return storeTypeName;
                }

                break;
        }

        if (postParens.Length == 0)
        {
            return preParens.Length == storeTypeName.Length
                ? storeTypeName
                : preParens.ToString();
        }

        return new StringBuilder(preParens.Length).Append(preParens).Append(postParens).ToString();
    }

    internal static void ParseStoreTypeName(
        string storeTypeName,
        out string name,
        out string? schema,
        out int? size,
        out int? precision,
        out int? scale)
    {
        var s = storeTypeName.AsSpan().Trim();
        var i = 0;
        size = precision = scale = null;

        if (s.EndsWith("[]", StringComparison.Ordinal))
        {
            // If this is an array store type, any facets (size, precision...) apply to the element and not to the array (e.g. varchar(32)[]
            // is an array mapping with Size=null over an element mapping of varchar with Size=32). So just add everything up to the end.
            // Note that if there's a schema (e.g. foo.varchar(32)[]), we return name=varchar(32), schema=foo.
            name = s.ToString();
            schema = null;
            return;
        }

        name = ParseNameComponent(s);

        if (i < s.Length && s[i] == '.')
        {
            i++;
            schema = name;
            name = ParseNameComponent(s);
        }
        else
        {
            schema = null;
        }

        s = s[i..];

        if (s.Length == 0 || s[0] != '(')
        {
            // No facets
            return;
        }

        s = s[1..];

        var closeParen = s.IndexOf(")", StringComparison.Ordinal);
        if (closeParen == -1)
        {
            return;
        }

        var inParens = s[..closeParen].Trim();
        // There may be stuff after the closing parentheses (e.g. timestamp(3) with time zone)
        var postParens = s.Slice(closeParen + 1);

        switch (s.IndexOf(",", StringComparison.Ordinal))
        {
            // No comma inside the parentheses, parse the value either as size or precision
            case -1:
                if (!int.TryParse(inParens, out var p))
                {
                    return;
                }

                if (NameBasesUsesPrecision(name))
                {
                    precision = p;
                    // scale = 0;
                }
                else
                {
                    size = p;
                }

                break;

            case var comma:
                if (int.TryParse(s[..comma].Trim(), out var parsedPrecision))
                {
                    precision = parsedPrecision;
                }
                else
                {
                    return;
                }

                if (int.TryParse(s[(comma + 1)..closeParen].Trim(), out var parsedScale))
                {
                    scale = parsedScale;
                }
                else
                {
                    return;
                }

                break;
        }

        if (postParens.Length > 0)
        {
            // There's stuff after the parentheses (e.g. time(3) with time zone), append to the name
            name += postParens.ToString();
        }

        string ParseNameComponent(ReadOnlySpan<char> s)
        {
            var inQuotes = false;
            StringBuilder builder = new();

            if (s[i] == '"')
            {
                inQuotes = true;
                i++;
            }

            var start = i;

            for (; i < s.Length; i++)
            {
                var c = s[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < s.Length && s[i + 1] == '"')
                        {
                            builder.Append('"');
                            i++;
                            continue;
                        }

                        i++;
                        break;
                    }
                }
                else if (!char.IsWhiteSpace(c) && !char.IsAsciiLetterOrDigit(c) && c != '_')
                {
                    break;
                }

                builder.Append(c);
            }

            var length = i - start;
            return length == storeTypeName.Length
                ? storeTypeName
                : builder.ToString();
        }
    }
}
