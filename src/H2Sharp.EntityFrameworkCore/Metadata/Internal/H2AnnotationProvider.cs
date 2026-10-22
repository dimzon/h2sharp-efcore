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

        if (modelIndex.GetMethod() is { } method)
        {
            yield return new Annotation(H2AnnotationNames.IndexMethod, method);
        }

        if (modelIndex.GetNullSortOrder() is { } nullSortOrder)
        {
            yield return new Annotation(H2AnnotationNames.IndexNullSortOrder, nullSortOrder);
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

        if (modelIndex.GetAreNullsDistinct() is { } nullsDistinct)
        {
            yield return new Annotation(H2AnnotationNames.NullsDistinct, nullsDistinct);
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
            a => a.Name.StartsWith(H2AnnotationNames.EnumPrefix, StringComparison.Ordinal));
    }
}
