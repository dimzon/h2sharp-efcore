using System.Collections;
using System.Globalization;
using System.Linq;
using H2Sharp.Data;
using System.Data;
using System.Data.Common;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;
using H2Sharp.EntityFrameworkCore.Storage.ValueConversion;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     Type mapping for H2 arrays.
/// </summary>
/// <remarks>
///     See: https://www.h2database.com/html/datatypes.html
/// </remarks>
public abstract class H2ArrayTypeMapping : RelationalTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2ArrayTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override RelationalTypeMapping WithTypeMappingInfo(in RelationalTypeMappingInfo mappingInfo)
    {
        var storeType = mappingInfo.StoreTypeName;
        if (storeType is not null && storeType.EndsWith("[]", StringComparison.Ordinal))
        {
            // A store type written with the brackets of PostgreSQL is read and
            // rewritten in the form H2 takes: the word ARRAY after the element type.
            storeType = storeType[..^2] + " ARRAY";
        }

        return Clone(
            new RelationalTypeMappingParameters(
                Parameters.CoreParameters,
                storeType ?? StoreType,
                StoreTypePostfix,
                mappingInfo.DbType ?? DbType,
                mappingInfo.IsUnicode ?? Parameters.Unicode,
                mappingInfo.Size ?? Size,
                mappingInfo.IsFixedLength ?? Parameters.FixedLength,
                mappingInfo.Precision ?? Precision,
                mappingInfo.Scale ?? Scale));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override RelationalTypeMapping ElementTypeMapping
    {
        get
        {
            var elementTypeMapping = base.ElementTypeMapping;
            Check.DebugAssert(
                elementTypeMapping is not null,
                "H2ArrayTypeMapping without an element type mapping");
            Check.DebugAssert(
                elementTypeMapping is RelationalTypeMapping,
                "H2ArrayTypeMapping with a non-relational element type mapping");
            return (RelationalTypeMapping)elementTypeMapping;
        }
    }
}

/// <summary>
///     Type mapping for H2 arrays.
/// </summary>
/// <remarks>
///     <para>
///         H2 2.5 writes an array as the type of its elements and the word ARRAY after it:
///         <c>INTEGER ARRAY</c>, <c>VARCHAR(20) ARRAY</c>, <c>INTEGER ARRAY[10]</c> for an array of
///         at most ten elements. The brackets of PostgreSQL (<c>integer[]</c>) H2 does not know.
///     </para>
///     <para>
///         The reference takes the form above; it was checked on the reference engine, and the
///         engine of this port does not read arrays yet, so a table with an array column cannot
///         be created yet. See docs/DIVERGENCES.md.
///     </para>
/// </remarks>
public class H2ArrayTypeMapping<TCollection, TConcreteCollection, TElement> : H2ArrayTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static H2ArrayTypeMapping<TCollection, TConcreteCollection, TElement> Default { get; }
        = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [UsedImplicitly]
    public H2ArrayTypeMapping(RelationalTypeMapping elementTypeMapping)
        : this(elementTypeMapping.StoreType + " ARRAY", elementTypeMapping)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [UsedImplicitly]
    public H2ArrayTypeMapping(string storeType, RelationalTypeMapping elementTypeMapping)
        : this(CreateParameters(NormalizeStoreType(storeType), elementTypeMapping))
    {
        Check.DebugAssert(
            storeType.EndsWith(" ARRAY", StringComparison.Ordinal)
            || storeType.EndsWith("[]", StringComparison.Ordinal),
            "H2ArrayTypeMapping created for a non-array store type: " + storeType);
    }

    /// <summary>
    ///     H2 writes an array as the type of its elements and the word ARRAY after it.
    ///     A store type written with the brackets of PostgreSQL ("integer[]") — from a
    ///     reverse-engineered catalogue or a user's explicit specification — is read and
    ///     rewritten in the form H2 takes.
    /// </summary>
    private static string NormalizeStoreType(string storeType)
        => storeType.EndsWith("[]", StringComparison.Ordinal)
            ? storeType[..^2] + " ARRAY"
            : storeType;

    private static RelationalTypeMappingParameters CreateParameters(string storeType, RelationalTypeMapping elementMapping)
    {
        ValueConverter? converter = null;

        // We do GetElementType for multidimensional arrays - these don't implement generic IEnumerable<>
        var elementType = typeof(TCollection).TryGetElementType(typeof(IEnumerable<>)) ?? typeof(TCollection).GetElementType();

        Check.DebugAssert(elementType is not null, "modelElementType cannot be null");

        if (elementMapping.Converter is { } elementConverter)
        {
            var providerElementType = elementConverter.ProviderClrType;

            // Nullability has been unwrapped on the element converter's provider CLR type, so add it back here if needed
            // object can hold null, and a non-nullable value-type provider element cannot carry
            // it: make it nullable the same way the reference provider does.
            if (elementType.IsNullableValueType()
                || elementType.UnwrapNullableType() == typeof(object) && providerElementType.IsValueType)
            {
                providerElementType = providerElementType.MakeNullable();
            }

            converter = (ValueConverter)Activator.CreateInstance(
                typeof(H2ArrayConverter<,,>).MakeGenericType(
                    typeof(TCollection), typeof(TConcreteCollection), providerElementType.MakeArrayType()),
                elementConverter)!;
        }
        // The ADO layer hands back an array for an array column, so a model collection that is a
        // list -- List<T> or IList<T> -- needs a converter to become one. Without it the shaper
        // asks the reader for the model type and the reader casts: "Unable to cast object of type
        // 'System.Int32[]' to type 'System.Collections.Generic.List`1[System.Int32]'".
        // A model type that is neither an array nor a list -- a key struct that happens to be
        // enumerable and carries a converter of its own -- keeps no converter, as before: the
        // converter below only knows arrays and IList<T>.
        else if (typeof(TCollection) != typeof(TConcreteCollection)
                 || typeof(TCollection).IsArrayOrGenericList() && !typeof(TCollection).IsArray)
        {
            converter = (ValueConverter)Activator.CreateInstance(
                typeof(H2ArrayConverter<,,>).MakeGenericType(
                    typeof(TCollection), typeof(TConcreteCollection), elementType.MakeArrayType()))!;
        }

#pragma warning disable EF1001
        var comparer = typeof(TCollection).IsArray && typeof(TCollection).GetArrayRank() > 1
            ? null // TODO: Value comparer for multidimensional arrays
            : (ValueComparer?)Activator.CreateInstance(
                elementType.IsNullableValueType()
                    ? typeof(ListOfNullableValueTypesComparer<,>)
                        .MakeGenericType(typeof(TConcreteCollection), elementType.UnwrapNullableType())
                    : elementType.IsValueType
                        ? typeof(ListOfValueTypesComparer<,>).MakeGenericType(typeof(TConcreteCollection), elementType)
                        : typeof(ListOfReferenceTypesComparer<,>).MakeGenericType(typeof(TConcreteCollection), elementType),
                elementMapping.Comparer.ToNullableComparer(elementType)!);
#pragma warning restore EF1001

        var elementJsonReaderWriter = elementMapping.JsonValueReaderWriter;
        if (elementJsonReaderWriter is not null && !typeof(TElement).UnwrapNullableType().IsAssignableTo(elementJsonReaderWriter.ValueType))
        {
            throw new InvalidOperationException(
                $"When building an array mapping over '{typeof(TElement).Name}', the JsonValueReaderWriter for element mapping '{elementMapping.GetType().Name}' is incorrect ('{elementJsonReaderWriter.ValueType.GetType().Name}' instead of '{typeof(TElement).UnwrapNullableType()}', the JsonValueReaderWriter is '{elementJsonReaderWriter.GetType().Name}').");
        }

        // If there's no JsonValueReaderWriter on the element, we also don't set one on its array (this is for rare edge cases such as
        // H2RowValueTypeMapping).
        // TODO: Also, we don't (yet) support JSON serialization of multidimensional arrays.
        var collectionJsonReaderWriter =
            elementJsonReaderWriter is null || typeof(TCollection).IsArray && typeof(TCollection).GetArrayRank() > 1
                ? null
                : (JsonValueReaderWriter?)Activator.CreateInstance(
                    (elementType.IsNullableValueType()
                        ? typeof(JsonCollectionOfNullableStructsReaderWriter<,>)
                        : elementType.IsValueType
                            ? typeof(JsonCollectionOfStructsReaderWriter<,>)
                            : typeof(JsonCollectionOfReferencesReaderWriter<,>))
                    .MakeGenericType(typeof(TConcreteCollection), elementType.UnwrapNullableType()),
                    elementJsonReaderWriter);

        return new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(
                typeof(TCollection), converter, comparer, elementMapping: elementMapping,
                jsonValueReaderWriter: collectionJsonReaderWriter),
            storeType);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2ArrayTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
        var clrType = parameters.CoreParameters.ClrType;

        if (clrType.TryGetElementType(typeof(IEnumerable<>)) == null && clrType.GetElementType() == null)
        {
            throw new ArgumentException($"CLR type '{parameters.CoreParameters.ClrType}' isn't an IEnumerable");
        }

        // Upstream this stashed a wire-protocol type code on the parameter, built from the element
        // mapping's provider type, so the driver would know which element type to send on the wire.
        // H2Sharp's ADO layer has no such code and infers from the value, so there is nothing to record
        // here. ConfigureParameter below pushes the element's DbType instead, which is what the engine
        // is given; whether an array parameter survives that round trip is a question for the tests,
        // not something to assert here.
    }

    // This constructor exists only to support the static Default property above, which is necessary to allow code generation for compiled
    // models. The constructor creates a completely blank type mapping, which will get cloned with all the correct details.
    private H2ArrayTypeMapping()
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(typeof(TCollection), elementMapping: NullMapping),
            "integer ARRAY"))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override DbParameter CreateParameter(
        DbCommand command,
        string name,
        object? value,
        bool? nullable = null,
        ParameterDirection direction = ParameterDirection.Input)
    {
        // EF infers the element CLR type from the comparison (Contains(o.OrderID) gives int), but
        // the query parameter can still be object[] or List<object>. Materialize it into the type
        // the converter expects before the converter's object-level sanitizer runs; that sanitizer
        // only knows Convert.ChangeType and cannot turn object[] into List<int>.
        if (value is not null && Converter is { } converter)
        {
            var modelType = converter.ModelClrType;
            if (!modelType.IsInstanceOfType(value) && value is IEnumerable values)
            {
                var elementType = modelType.TryGetElementType(typeof(IEnumerable<>))
                    ?? modelType.GetElementType()
                    ?? typeof(TElement);
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

                foreach (var item in values)
                {
                    list.Add(item is null || elementType.IsInstanceOfType(item)
                        ? item
                        : Convert.ChangeType(item, elementType, CultureInfo.InvariantCulture));
                }

                if (modelType.IsArray)
                {
                    var array = Array.CreateInstance(elementType, list.Count);
                    list.CopyTo(array, 0);
                    value = array;
                }
                else
                {
                    value = list;
                }
            }
        }
        // Queries which compose non-server-correlated LINQ operators over an array parameter
        // (ids.Skip(1)) hand us an enumerable that isn't an array/list. With no converter we can
        // safely evaluate it into a List of the mapping's element type.
        // TODO: Make H2 support IList<> instead of only arrays and List<>
        else if (value is not null && !value.GetType().IsArrayOrGenericList())
        {
            switch (value)
            {
                case IEnumerable<TElement> elements:
                    value = elements.ToList();
                    break;

                case IEnumerable elements:
                    value = elements.Cast<TElement>().ToList();
                    break;
            }
        }

        // The engine reads a byte[] as a binary string (varbinary), which is what a byte[] means
        // in every other place. An array of bytes is the one place where the CLR type is
        // ambiguous: when the element of the array is byte, the value is sent as the array of
        // small ints the column expects.
        if (value is byte[] bytes && typeof(TElement) == typeof(byte))
        {
            value = bytes.Select(b => (short)b).ToArray();
        }

        var param = base.CreateParameter(command, name, value, nullable, direction);
        if (param is not H2Parameter)
        {
            throw new InvalidOperationException(
                $"H2-specific type mapping {GetType().Name} being used with non-H2 parameter type {param.GetType().Name}");
        }

        // Nothing more is set on the parameter. Upstream this named the PostgreSQL element type in
        // DataTypeName, which an array of enums and of user-defined ranges needed; H2Sharp infers
        // the element type from the value it is handed, and the mapping pins it in DDL instead.
        return param;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
    {
        Check.DebugAssert(
            parameters.CoreParameters.ClrType == typeof(TCollection), "H2ArrayTypeMapping.Clone attempting to change ClrType");
        Check.DebugAssert(
            parameters.CoreParameters.ElementTypeMapping is not null, "H2ArrayTypeMapping.Clone without an element type mapping");
        Check.DebugAssert(
            parameters.CoreParameters.ElementTypeMapping.ClrType == typeof(TElement).UnwrapNullableType(),
            "H2ArrayTypeMapping.Clone attempting to change element ClrType");

        return new H2ArrayTypeMapping<TCollection, TConcreteCollection, TElement>(parameters);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
    {
        if (value is not IEnumerable enumerable)
        {
            throw new ArgumentException($"'{value.GetType().Name}' must be an IEnumerable", nameof(value));
        }

        if (value is Array array && array.Rank != 1)
        {
            throw new NotSupportedException("Multidimensional array literals aren't supported");
        }

        // No cast after the brackets. PostgreSQL needs one to say what the array holds
        // ("ARRAY[1,2]::integer[]"), and H2 2.5 has no such cast at all: the short form
        // "x::y" is not in the grammar, and the type of the array follows from the
        // elements and from the place the array stands in. The reference takes
        // "ARRAY[1,2,3]" as it is.
        var literals = new List<string>();
        foreach (var element in enumerable)
        {
            literals.Add(ElementTypeMapping.GenerateProviderValueSqlLiteral(element));
        }

        return Dialect.AppendArray(new StringBuilder(), literals, static (sb, literal) => sb.Append(literal)).ToString();
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
            throw new ArgumentException(
                $"H2-specific type mapping {GetType()} being used with non-H2 parameter type {parameter.GetType().Name}");
        }

        // An H2 array is typed by its element. The wrapper carries that type explicitly, so a
        // byte[] holding sbyte values is not mistaken for VARBINARY by the engine.
        if (ElementTypeMapping.DbType is { } elementDbType)
        {
            ((H2Parameter)parameter).DbType = elementDbType;
        }

        // Only the ambiguous shape needs the wrapper: byte[] carrying sbyte values. Other
        // collection values already reach the engine with their CLR type intact.
        if (ElementTypeMapping.ClrType == typeof(sbyte) && parameter.Value is byte[] bytes)
        {
            parameter.Value = new H2EfArrayParameterValue(typeof(sbyte), bytes.Select(b => unchecked((sbyte)b)));
        }
    }
}
