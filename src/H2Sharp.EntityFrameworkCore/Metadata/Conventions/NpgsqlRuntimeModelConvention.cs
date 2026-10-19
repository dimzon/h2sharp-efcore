using H2Sharp.EntityFrameworkCore.Metadata.Internal;

namespace H2Sharp.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     A convention that creates an optimized copy of the mutable model.
/// </summary>
public class NpgsqlRuntimeModelConvention : RelationalRuntimeModelConvention
{
    /// <summary>
    ///     Creates a new instance of <see cref="NpgsqlRuntimeModelConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    /// <param name="relationalDependencies">Parameter object containing relational dependencies for this convention.</param>
    public NpgsqlRuntimeModelConvention(
        ProviderConventionSetBuilderDependencies dependencies,
        RelationalConventionSetBuilderDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <inheritdoc />
    protected override void ProcessModelAnnotations(
        Dictionary<string, object?> annotations,
        IModel model,
        RuntimeModel runtimeModel,
        bool runtime)
    {
        base.ProcessModelAnnotations(annotations, model, runtimeModel, runtime);

        if (!runtime)
        {
            annotations.Remove(H2AnnotationNames.DatabaseTemplate);
            annotations.Remove(H2AnnotationNames.Tablespace);
            annotations.Remove(H2AnnotationNames.CollationDefinitionPrefix);

#pragma warning disable CS0618
            annotations.Remove(H2AnnotationNames.DefaultColumnCollation);
#pragma warning restore CS0618

            foreach (var annotationName in annotations.Keys.Where(
                         k =>
                             k.StartsWith(H2AnnotationNames.PostgresExtensionPrefix, StringComparison.Ordinal)
                             || k.StartsWith(H2AnnotationNames.EnumPrefix, StringComparison.Ordinal)
                             || k.StartsWith(H2AnnotationNames.RangePrefix, StringComparison.Ordinal)))
            {
                annotations.Remove(annotationName);
            }
        }
    }

    /// <inheritdoc />
    protected override void ProcessEntityTypeAnnotations(
        Dictionary<string, object?> annotations,
        IEntityType entityType,
        RuntimeEntityType runtimeEntityType,
        bool runtime)
    {
        base.ProcessEntityTypeAnnotations(annotations, entityType, runtimeEntityType, runtime);

        if (!runtime)
        {
            annotations.Remove(H2AnnotationNames.UnloggedTable);

            foreach (var annotationName in annotations.Keys.Where(
                         k => k.StartsWith(H2AnnotationNames.StorageParameterPrefix, StringComparison.Ordinal)))
            {
                annotations.Remove(annotationName);
            }
        }
    }

    /// <inheritdoc />
    protected override void ProcessPropertyAnnotations(
        Dictionary<string, object?> annotations,
        IProperty property,
        RuntimeProperty runtimeProperty,
        bool runtime)
    {
        base.ProcessPropertyAnnotations(annotations, property, runtimeProperty, runtime);

        if (!runtime)
        {
            annotations.Remove(H2AnnotationNames.IdentityOptions);
            annotations.Remove(H2AnnotationNames.TsVectorConfig);
            annotations.Remove(H2AnnotationNames.TsVectorProperties);

            if (!annotations.ContainsKey(H2AnnotationNames.ValueGenerationStrategy))
            {
                annotations[H2AnnotationNames.ValueGenerationStrategy] = property.GetValueGenerationStrategy();
            }
        }
    }

    /// <inheritdoc />
    protected override void ProcessIndexAnnotations(
        Dictionary<string, object?> annotations,
        IIndex index,
        RuntimeIndex runtimeIndex,
        bool runtime)
    {
        base.ProcessIndexAnnotations(annotations, index, runtimeIndex, runtime);

        if (!runtime)
        {
            annotations.Remove(H2AnnotationNames.IndexMethod);
            annotations.Remove(H2AnnotationNames.IndexOperators);
            annotations.Remove(H2AnnotationNames.IndexSortOrder);
            annotations.Remove(H2AnnotationNames.IndexNullSortOrder);
            annotations.Remove(H2AnnotationNames.IndexInclude);
            annotations.Remove(H2AnnotationNames.CreatedConcurrently);
            annotations.Remove(H2AnnotationNames.NullsDistinct);

            foreach (var annotationName in annotations.Keys.Where(
                         k => k.StartsWith(H2AnnotationNames.StorageParameterPrefix, StringComparison.Ordinal)))
            {
                annotations.Remove(annotationName);
            }
        }
    }
}
