// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Design.Internal;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

namespace H2Sharp.EntityFrameworkCore.Design.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage.
public class H2CSharpRuntimeAnnotationCodeGenerator
    : RelationalCSharpRuntimeAnnotationCodeGenerator, ICSharpRuntimeAnnotationCodeGenerator
{
    private int _typeMappingNestingCount;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2CSharpRuntimeAnnotationCodeGenerator(
        CSharpRuntimeAnnotationCodeGeneratorDependencies dependencies,
        RelationalCSharpRuntimeAnnotationCodeGeneratorDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool Create(
        CoreTypeMapping typeMapping,
        CSharpRuntimeAnnotationCodeGeneratorParameters parameters,
        ValueComparer? valueComparer = null,
        ValueComparer? keyValueComparer = null,
        ValueComparer? providerValueComparer = null)
    {
        _typeMappingNestingCount++;

        try
        {
            var result = base.Create(typeMapping, parameters, valueComparer, keyValueComparer, providerValueComparer);
            AddH2TypeMappingTweaks(typeMapping, parameters);
            return result;
        }
        finally
        {
            _typeMappingNestingCount--;
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [EntityFrameworkInternal]
    protected virtual void AddH2TypeMappingTweaks(
        CoreTypeMapping typeMapping,
        CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        var mainBuilder = parameters.MainBuilder;

        // Upstream this also emitted a Clone(npgsqlDbType: ...) call for any mapping whose provider
        // type enum differed from the default instance. There is no such enum here, so the only
        // mapping that still needs its store type pinned into the compiled model is an enum type,
        // whose labels have to travel with it.

        switch (typeMapping)
        {
            case H2EnumTypeMapping enumTypeMapping:
                CheckElementTypeMapping();

                var code = Dependencies.CSharpHelper;
                mainBuilder.AppendLine(";");

                mainBuilder.AppendLine(
                        $"{parameters.TargetName}.TypeMapping = ((H2EnumTypeMapping){parameters.TargetName}.TypeMapping).Clone(")
                    .IncrementIndent();

                mainBuilder
                    .Append("unquotedStoreType: ")
                    .Append(code.Literal(enumTypeMapping.UnquotedStoreType))
                    .AppendLine(",")
                    .AppendLine("labels: new Dictionary<object, string>()")
                    .AppendLine("{")
                    .IncrementIndent();

                foreach (var (enumValue, label) in enumTypeMapping.Labels)
                {
                    mainBuilder
                        .Append('[')
                        .Append(code.UnknownLiteral(enumValue))
                        .Append(']')
                        .Append(" = ")
                        .Append(code.Literal(label))
                        .AppendLine(",");
                }

                mainBuilder
                    .Append("}")
                    .DecrementIndent()
                    .Append(")")
                    .DecrementIndent();

                break;

        }

        void CheckElementTypeMapping()
        {
            if (_typeMappingNestingCount > 1)
            {
                throw new NotSupportedException(
                    $"Non-default H2 type mappings ('{typeMapping.GetType().Name}' with store type '{(typeMapping as RelationalTypeMapping)?.StoreType}') aren't currently supported as element type mappings, see https://github.com/npgsql/efcore.pg/issues/3366.");
            }
        }
    }

    /// <inheritdoc />
    public override void Generate(IModel model, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;
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

        base.Generate(model, parameters);
    }

    /// <inheritdoc />
    public override void Generate(IRelationalModel model, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;
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

        base.Generate(model, parameters);
    }

    /// <inheritdoc />
    public override void Generate(IProperty property, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;
            annotations.Remove(H2AnnotationNames.IdentityOptions);
            annotations.Remove(H2AnnotationNames.TsVectorConfig);
            annotations.Remove(H2AnnotationNames.TsVectorProperties);
            annotations.Remove(H2AnnotationNames.CompressionMethod);

            if (!annotations.ContainsKey(H2AnnotationNames.ValueGenerationStrategy))
            {
                annotations[H2AnnotationNames.ValueGenerationStrategy] = property.GetValueGenerationStrategy();
            }
        }

        base.Generate(property, parameters);
    }

    /// <inheritdoc />
    public override void Generate(IColumn column, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;

            annotations.Remove(H2AnnotationNames.IdentityOptions);
            annotations.Remove(H2AnnotationNames.TsVectorConfig);
            annotations.Remove(H2AnnotationNames.TsVectorProperties);
            annotations.Remove(H2AnnotationNames.CompressionMethod);
        }

        base.Generate(column, parameters);
    }

    /// <inheritdoc />
    public override void Generate(IIndex index, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;

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

        base.Generate(index, parameters);
    }

    /// <inheritdoc />
    public override void Generate(ITableIndex index, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;

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

        base.Generate(index, parameters);
    }

    /// <inheritdoc />
    public override void Generate(IEntityType entityType, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;

            foreach (var annotationName in annotations.Keys.Where(
                         k => k.StartsWith(H2AnnotationNames.StorageParameterPrefix, StringComparison.Ordinal)))
            {
                annotations.Remove(annotationName);
            }
        }

        base.Generate(entityType, parameters);
    }

    /// <inheritdoc />
    public override void Generate(ITable table, CSharpRuntimeAnnotationCodeGeneratorParameters parameters)
    {
        if (!parameters.IsRuntime)
        {
            var annotations = parameters.Annotations;

            foreach (var annotationName in annotations.Keys.Where(
                         k => k.StartsWith(H2AnnotationNames.StorageParameterPrefix, StringComparison.Ordinal)))
            {
                annotations.Remove(annotationName);
            }
        }

        base.Generate(table, parameters);
    }
}
