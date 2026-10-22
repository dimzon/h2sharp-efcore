using System.Diagnostics.CodeAnalysis;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;

namespace H2Sharp.EntityFrameworkCore.Design.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2AnnotationCodeGenerator : AnnotationCodeGenerator
{
    #region MethodInfos

    private static readonly MethodInfo ModelHasH2EnumMethodInfo1
        = typeof(H2ModelBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2ModelBuilderExtensions.HasH2Enum), typeof(ModelBuilder), typeof(string), typeof(string[]));

    private static readonly MethodInfo ModelHasH2EnumMethodInfo2
        = typeof(H2ModelBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2ModelBuilderExtensions.HasH2Enum), typeof(ModelBuilder), typeof(string), typeof(string), typeof(string[]));

    private static readonly MethodInfo ModelUseSerialColumnsMethodInfo
        = typeof(H2ModelBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2ModelBuilderExtensions.UseSerialColumns), typeof(ModelBuilder));

    private static readonly MethodInfo ModelUseIdentityAlwaysColumnsMethodInfo
        = typeof(H2ModelBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2ModelBuilderExtensions.UseIdentityAlwaysColumns), typeof(ModelBuilder));

    private static readonly MethodInfo ModelUseIdentityByDefaultColumnsMethodInfo
        = typeof(H2ModelBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2ModelBuilderExtensions.UseIdentityByDefaultColumns), typeof(ModelBuilder));

    private static readonly MethodInfo ModelUseHiLoMethodInfo
        = typeof(H2ModelBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2ModelBuilderExtensions.UseHiLo), typeof(ModelBuilder), typeof(string), typeof(string));

    private static readonly MethodInfo ModelHasAnnotationMethodInfo
        = typeof(ModelBuilder).GetRequiredRuntimeMethod(
            nameof(ModelBuilder.HasAnnotation), typeof(string), typeof(object));

    private static readonly MethodInfo ModelUseKeySequencesMethodInfo
        = typeof(H2ModelBuilderExtensions).GetRuntimeMethod(
            nameof(H2ModelBuilderExtensions.UseKeySequences), [typeof(ModelBuilder), typeof(string), typeof(string)])!;

    private static readonly MethodInfo PropertyUseSerialColumnMethodInfo
        = typeof(H2PropertyBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2PropertyBuilderExtensions.UseSerialColumn), typeof(PropertyBuilder));

    private static readonly MethodInfo PropertyUseIdentityAlwaysColumnMethodInfo
        = typeof(H2PropertyBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2PropertyBuilderExtensions.UseIdentityAlwaysColumn), typeof(PropertyBuilder));

    private static readonly MethodInfo PropertyUseIdentityByDefaultColumnMethodInfo
        = typeof(H2PropertyBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2PropertyBuilderExtensions.UseIdentityByDefaultColumn), typeof(PropertyBuilder));

    private static readonly MethodInfo PropertyUseHiLoMethodInfo
        = typeof(H2PropertyBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2PropertyBuilderExtensions.UseHiLo), typeof(PropertyBuilder), typeof(string), typeof(string));

    private static readonly MethodInfo PropertyHasIdentityOptionsMethodInfo
        = typeof(H2PropertyBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2PropertyBuilderExtensions.HasIdentityOptions), typeof(PropertyBuilder), typeof(long?), typeof(long?),
            typeof(long?), typeof(long?), typeof(bool?), typeof(long?));

    private static readonly MethodInfo PropertyUseSequenceMethodInfo
        = typeof(H2PropertyBuilderExtensions).GetRuntimeMethod(
            nameof(H2PropertyBuilderExtensions.UseSequence), [typeof(PropertyBuilder), typeof(string), typeof(string)])!;

    private static readonly MethodInfo IndexHasMethodMethodInfo
        = typeof(H2IndexBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2IndexBuilderExtensions.HasMethod), typeof(IndexBuilder), typeof(string));

    private static readonly MethodInfo IndexHasNullSortOrderMethodInfo
        = typeof(H2IndexBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2IndexBuilderExtensions.HasNullSortOrder), typeof(IndexBuilder), typeof(NullSortOrder[]));

    private static readonly MethodInfo IndexIncludePropertiesMethodInfo
        = typeof(H2IndexBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2IndexBuilderExtensions.IncludeProperties), typeof(IndexBuilder), typeof(string[]));

    private static readonly MethodInfo IndexAreNullsDistinctMethodInfo
        = typeof(H2IndexBuilderExtensions).GetRequiredRuntimeMethod(
            nameof(H2IndexBuilderExtensions.AreNullsDistinct), typeof(IndexBuilder), typeof(bool));

    #endregion MethodInfos

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2AnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool IsHandledByConvention(IModel model, IAnnotation annotation)
    {
        Check.NotNull(model, nameof(model));
        Check.NotNull(annotation, nameof(annotation));

        if (annotation.Name == RelationalAnnotationNames.DefaultSchema
            && (string?)annotation.Value == "public")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool IsHandledByConvention(IIndex index, IAnnotation annotation)
    {
        Check.NotNull(index, nameof(index));
        Check.NotNull(annotation, nameof(annotation));

        if (annotation.Name == H2AnnotationNames.IndexMethod
            && (string?)annotation.Value == "btree")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool IsHandledByConvention(IProperty property, IAnnotation annotation)
    {
        Check.NotNull(property, nameof(property));
        Check.NotNull(annotation, nameof(annotation));

        // The default by-convention value generation strategy is serial in pre-10 PostgreSQL,
        // and IdentityByDefault otherwise.
        if (annotation.Name == H2AnnotationNames.ValueGenerationStrategy)
        {
            // Note: both serial and identity-by-default columns are considered by-convention - we don't want
            // to assume that the PostgreSQL version of the scaffolded database necessarily determines the
            // version of the database that the scaffolded model will target. This makes life difficult for
            // models with mixed strategies but that's an edge case.
            return (H2ValueGenerationStrategy?)annotation.Value switch
            {
                H2ValueGenerationStrategy.SerialColumn => true,
                H2ValueGenerationStrategy.IdentityByDefaultColumn => true,
                _ => false
            };
        }

        return false;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IModel model,
        IDictionary<string, IAnnotation> annotations)
    {
        var fragments = new List<MethodCallCodeFragment>(base.GenerateFluentApiCalls(model, annotations));

        if (GenerateValueGenerationStrategy(annotations, onModel: true) is { } valueGenerationStrategy)
        {
            fragments.Add(valueGenerationStrategy);
        }

        return fragments;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override MethodCallCodeFragment? GenerateFluentApi(IModel model, IAnnotation annotation)
    {
        Check.NotNull(model, nameof(model));
        Check.NotNull(annotation, nameof(annotation));

        if (annotation.Name.StartsWith(H2AnnotationNames.EnumPrefix, StringComparison.Ordinal))
        {
            var enumTypeDef = new H2Enum(model, annotation.Name);

            return enumTypeDef.Schema is null
                ? new MethodCallCodeFragment(ModelHasH2EnumMethodInfo1, enumTypeDef.Name, enumTypeDef.Labels)
                : new MethodCallCodeFragment(ModelHasH2EnumMethodInfo2, enumTypeDef.Schema, enumTypeDef.Name, enumTypeDef.Labels);
        }

        return null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override MethodCallCodeFragment? GenerateFluentApi(IEntityType entityType, IAnnotation annotation)
    {
        Check.NotNull(entityType, nameof(entityType));
        Check.NotNull(annotation, nameof(annotation));

        return null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IProperty property,
        IDictionary<string, IAnnotation> annotations)
    {
        var fragments = new List<MethodCallCodeFragment>(base.GenerateFluentApiCalls(property, annotations));

        if (GenerateValueGenerationStrategy(annotations, onModel: false) is { } valueGenerationStrategy)
        {
            fragments.Add(valueGenerationStrategy);
        }

        if (GenerateIdentityOptions(annotations) is { } identityOptionsFragment)
        {
            fragments.Add(identityOptionsFragment);
        }

        return fragments;
    }

    private MethodCallCodeFragment? GenerateValueGenerationStrategy(IDictionary<string, IAnnotation> annotations, bool onModel)
    {
        if (!TryGetAndRemove(annotations, H2AnnotationNames.ValueGenerationStrategy, out H2ValueGenerationStrategy strategy))
        {
            return null;
        }

        switch (strategy)
        {
            case H2ValueGenerationStrategy.SerialColumn:
                return new MethodCallCodeFragment(onModel ? ModelUseSerialColumnsMethodInfo : PropertyUseSerialColumnMethodInfo);

            case H2ValueGenerationStrategy.IdentityAlwaysColumn:
                return new MethodCallCodeFragment(
                    onModel ? ModelUseIdentityAlwaysColumnsMethodInfo : PropertyUseIdentityAlwaysColumnMethodInfo);

            case H2ValueGenerationStrategy.IdentityByDefaultColumn:
                return new MethodCallCodeFragment(
                    onModel ? ModelUseIdentityByDefaultColumnsMethodInfo : PropertyUseIdentityByDefaultColumnMethodInfo);

            case H2ValueGenerationStrategy.SequenceHiLo:
            {
                var name = GetAndRemove<string>(H2AnnotationNames.HiLoSequenceName)!;
                var schema = GetAndRemove<string>(H2AnnotationNames.HiLoSequenceSchema);
                return new MethodCallCodeFragment(
                    onModel ? ModelUseHiLoMethodInfo : PropertyUseHiLoMethodInfo,
                    (name, schema) switch
                    {
                        (null, null) => [],
                        (_, null) => [name],
                        _ => [name!, schema]
                    });
            }

            case H2ValueGenerationStrategy.Sequence:
            {
                var nameOrSuffix = GetAndRemove<string>(
                    onModel ? H2AnnotationNames.SequenceNameSuffix : H2AnnotationNames.SequenceName);

                var schema = GetAndRemove<string>(H2AnnotationNames.SequenceSchema);
                return new MethodCallCodeFragment(
                    onModel ? ModelUseKeySequencesMethodInfo : PropertyUseSequenceMethodInfo,
                    (name: nameOrSuffix, schema) switch
                    {
                        (null, null) => [],
                        (_, null) => [nameOrSuffix],
                        _ => [nameOrSuffix!, schema]
                    });
            }
            case H2ValueGenerationStrategy.None:
                return new MethodCallCodeFragment(
                    ModelHasAnnotationMethodInfo, H2AnnotationNames.ValueGenerationStrategy, H2ValueGenerationStrategy.None);

            default:
                throw new ArgumentOutOfRangeException(strategy.ToString());
        }

        T? GetAndRemove<T>(string annotationName)
            => TryGetAndRemove(annotations, annotationName, out T? annotationValue)
                ? annotationValue
                : default;
    }

    private MethodCallCodeFragment? GenerateIdentityOptions(IDictionary<string, IAnnotation> annotations)
    {
        if (!TryGetAndRemove(
                annotations, H2AnnotationNames.IdentityOptions,
                out string? annotationValue))
        {
            return null;
        }

        var identityOptions = IdentitySequenceOptionsData.Deserialize(annotationValue);
        return new MethodCallCodeFragment(
            PropertyHasIdentityOptionsMethodInfo,
            identityOptions.StartValue,
            identityOptions.IncrementBy == 1 ? null : (long?)identityOptions.IncrementBy,
            identityOptions.MinValue,
            identityOptions.MaxValue,
            identityOptions.IsCyclic ? true : null,
            identityOptions.NumbersToCache == 1 ? null : (long?)identityOptions.NumbersToCache);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override MethodCallCodeFragment? GenerateFluentApi(IIndex index, IAnnotation annotation)
        => annotation.Name switch
        {
    
            H2AnnotationNames.IndexMethod
                => new MethodCallCodeFragment(IndexHasMethodMethodInfo, annotation.Value),
            H2AnnotationNames.IndexNullSortOrder
                => new MethodCallCodeFragment(IndexHasNullSortOrderMethodInfo, annotation.Value),
            H2AnnotationNames.IndexInclude
                => new MethodCallCodeFragment(IndexIncludePropertiesMethodInfo, annotation.Value),
            H2AnnotationNames.NullsDistinct
                => new MethodCallCodeFragment(IndexAreNullsDistinctMethodInfo, annotation.Value),
            _ => null
        };

    private static bool TryGetAndRemove<T>(
        IDictionary<string, IAnnotation> annotations,
        string annotationName,
        [NotNullWhen(true)] out T? annotationValue)
    {
        if (annotations.TryGetValue(annotationName, out var annotation)
            && annotation.Value is not null)
        {
            annotations.Remove(annotationName);
            annotationValue = (T)annotation.Value;
            return true;
        }

        annotationValue = default;
        return false;
    }
}
