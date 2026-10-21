using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace H2Sharp.EntityFrameworkCore.Metadata.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2AnnotationProvider : RelationalAnnotationProvider
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2AnnotationProvider(RelationalAnnotationProviderDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        // Model validation ensures that these facets are the same on all mapped entity types
        var entityType = (IEntityType)table.EntityTypeMappings.First().TypeBase;

        foreach (var storageParamAnnotation in entityType.GetAnnotations()
                     .Where(a => a.Name.StartsWith(H2AnnotationNames.StorageParameterPrefix, StringComparison.Ordinal)))
        {
            yield return storageParamAnnotation;
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
        var valueGeneratedProperty = column.PropertyMappings.Where(
                m => (m.TableMapping.IsSharedTablePrincipal ?? true)
                    && m.TableMapping.TypeBase == m.Property.DeclaringType)
            .Select(m => m.Property)
            .FirstOrDefault(
                p => p.GetValueGenerationStrategy(table) switch
                {
                    H2ValueGenerationStrategy.IdentityByDefaultColumn => true,
                    H2ValueGenerationStrategy.IdentityAlwaysColumn => true,
                    H2ValueGenerationStrategy.SerialColumn => true,
                    _ => false
                });

        if (valueGeneratedProperty is not null)
        {
            var valueGenerationStrategy = valueGeneratedProperty.GetValueGenerationStrategy();
            yield return new Annotation(H2AnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);

            if (valueGenerationStrategy is H2ValueGenerationStrategy.IdentityByDefaultColumn
                or H2ValueGenerationStrategy.IdentityAlwaysColumn)
            {
                if (valueGeneratedProperty[H2AnnotationNames.IdentityOptions] is string identityOptions)
                {
                    yield return new Annotation(H2AnnotationNames.IdentityOptions, identityOptions);
                }
            }
        }

        if (column.PropertyMappings.Select(m => m.Property.GetTsVectorConfig())
                .FirstOrDefault(c => c is not null) is { } tsVectorConfig)
        {
            yield return new Annotation(H2AnnotationNames.TsVectorConfig, tsVectorConfig);
        }

        valueGeneratedProperty = column.PropertyMappings.Select(m => m.Property)
            .FirstOrDefault(p => p.GetTsVectorProperties() is not null);
        if (valueGeneratedProperty is not null)
        {
            var tableIdentifier = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);

            yield return new Annotation(
                H2AnnotationNames.TsVectorProperties,
                valueGeneratedProperty.GetTsVectorProperties()!
                    .Select(p2 => valueGeneratedProperty.DeclaringType.FindProperty(p2)!.GetColumnName(tableIdentifier))
                    .ToArray());
        }

        // JSON columns have no property mappings so all annotations that rely on property mappings should be skipped for them
        if (column is not JsonColumn
            && column.PropertyMappings.FirstOrDefault()?.Property.GetCompressionMethod() is { } compressionMethod)
        {
            // Model validation ensures that these facets are the same on all mapped properties
            yield return new Annotation(H2AnnotationNames.CompressionMethod, compressionMethod);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(ITableIndex index, bool designTime)
    {
        if (!designTime)
        {
            yield break;
        }

        // Model validation ensures that these facets are the same on all mapped indexes
        var modelIndex = index.MappedIndexes.First();

        if (modelIndex.GetCollation() is { } collation)
        {
            yield return new Annotation(RelationalAnnotationNames.Collation, collation);
        }

        if (modelIndex.GetMethod() is { } method)
        {
            yield return new Annotation(H2AnnotationNames.IndexMethod, method);
        }

        if (modelIndex.GetOperators() is { } operators)
        {
            yield return new Annotation(H2AnnotationNames.IndexOperators, operators);
        }

        if (modelIndex.GetNullSortOrder() is { } nullSortOrder)
        {
            yield return new Annotation(H2AnnotationNames.IndexNullSortOrder, nullSortOrder);
        }

        if (modelIndex.GetTsVectorConfig() is { } configName)
        {
            yield return new Annotation(H2AnnotationNames.TsVectorConfig, configName);
        }

        if (modelIndex.GetIncludeProperties() is { } includeProperties)
        {
            var tableIdentifier = StoreObjectIdentifier.Table(index.Table.Name, index.Table.Schema);

            yield return new Annotation(
                H2AnnotationNames.IndexInclude,
                includeProperties
                    .Select(p => modelIndex.DeclaringEntityType.FindProperty(p)!.GetColumnName(tableIdentifier))
                    .ToArray());
        }

        if (modelIndex.IsCreatedConcurrently() is { } isCreatedConcurrently)
        {
            yield return new Annotation(H2AnnotationNames.CreatedConcurrently, isCreatedConcurrently);
        }

        if (modelIndex.GetAreNullsDistinct() is { } nullsDistinct)
        {
            yield return new Annotation(H2AnnotationNames.NullsDistinct, nullsDistinct);
        }

        foreach (var storageParamAnnotation in modelIndex.GetAnnotations()
                     .Where(a => a.Name.StartsWith(H2AnnotationNames.StorageParameterPrefix, StringComparison.Ordinal)))
        {
            yield return storageParamAnnotation;
        }

        // Support legacy annotation for index ordering
        if (modelIndex[H2AnnotationNames.IndexSortOrder] is IReadOnlyList<SortOrder> legacySortOrder)
        {
            yield return new Annotation(H2AnnotationNames.IndexSortOrder, legacySortOrder);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
    {
        if (!designTime)
        {
            return [];
        }

        return model.Model.GetAnnotations().Where(
            a =>
                a.Name.StartsWith(H2AnnotationNames.PostgresExtensionPrefix, StringComparison.Ordinal)
                || a.Name.StartsWith(H2AnnotationNames.EnumPrefix, StringComparison.Ordinal)
                || a.Name.StartsWith(H2AnnotationNames.RangePrefix, StringComparison.Ordinal)
                || a.Name.StartsWith(H2AnnotationNames.CollationDefinitionPrefix, StringComparison.Ordinal));
    }
}
