using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Metadata;

namespace H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2ModelValidator : RelationalModelValidator
{
    /// <summary>
    ///     The backend version to target.
    /// </summary>

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2ModelValidator(
        ModelValidatorDependencies dependencies,
        RelationalModelValidatorDependencies relationalDependencies,
        IH2SingletonOptions npgsqlSingletonOptions)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        base.Validate(model, logger);

        ValidateIndexIncludeProperties(model);
    }


    /// <inheritdoc/>
    protected override void ValidateTypeMappings(
        IModel model,
        IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        base.ValidateTypeMappings(model, logger);

        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetFlattenedDeclaredProperties())
            {
                var strategy = property.GetValueGenerationStrategy();
                var propertyType = property.ClrType;

                switch (strategy)
                {
                    case H2ValueGenerationStrategy.None:
                        break;

                    case H2ValueGenerationStrategy.IdentityByDefaultColumn:
                    case H2ValueGenerationStrategy.IdentityAlwaysColumn:
                        if (!H2PropertyExtensions.IsCompatibleWithValueGeneration(property))
                        {
                            throw new InvalidOperationException(
                                H2Strings.IdentityBadType(
                                    property.Name, property.DeclaringType.DisplayName(), propertyType.ShortDisplayName()));
                        }

                        break;

                    case H2ValueGenerationStrategy.SequenceHiLo:
                    case H2ValueGenerationStrategy.Sequence:
                    case H2ValueGenerationStrategy.SerialColumn:
                        if (!H2PropertyExtensions.IsCompatibleWithValueGeneration(property))
                        {
                            throw new InvalidOperationException(
                                H2Strings.SequenceBadType(
                                    property.Name, property.DeclaringType.DisplayName(), propertyType.ShortDisplayName()));
                        }

                        break;

                    default:
                        throw new UnreachableException();
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
    protected virtual void ValidateIndexIncludeProperties(IModel model)
    {
        foreach (var index in model.GetEntityTypes().SelectMany(t => t.GetDeclaredIndexes()))
        {
            var includeProperties = index.GetIncludeProperties();
            if (includeProperties?.Count > 0)
            {
                var notFound = includeProperties
                    .FirstOrDefault(i => index.DeclaringEntityType.FindProperty(i) is null);

                if (notFound is not null)
                {
                    throw new InvalidOperationException(
                        H2Strings.IncludePropertyNotFound(index.DeclaringEntityType.DisplayName(), notFound));
                }

                var duplicate = includeProperties
                    .GroupBy(i => i)
                    .Where(g => g.Count() > 1)
                    .Select(y => y.Key)
                    .FirstOrDefault();

                if (duplicate is not null)
                {
                    throw new InvalidOperationException(
                        H2Strings.IncludePropertyDuplicated(index.DeclaringEntityType.DisplayName(), duplicate));
                }

                var inIndex = includeProperties
                    .FirstOrDefault(i => index.Properties.Any(p => i == p.Name));

                if (inIndex is not null)
                {
                    throw new InvalidOperationException(
                        H2Strings.IncludePropertyInIndex(index.DeclaringEntityType.DisplayName(), inIndex));
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void ValidateStoredProcedures(
        IModel model,
        IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
        base.ValidateStoredProcedures(model, logger);

        foreach (var entityType in model.GetEntityTypes())
        {
            if (entityType.GetDeleteStoredProcedure() is { } deleteStoredProcedure)
            {
                ValidateSproc(deleteStoredProcedure, logger);
            }

            if (entityType.GetInsertStoredProcedure() is { } insertStoredProcedure)
            {
                ValidateSproc(insertStoredProcedure, logger);
            }

            if (entityType.GetUpdateStoredProcedure() is { } updateStoredProcedure)
            {
                ValidateSproc(updateStoredProcedure, logger);
            }
        }

        static void ValidateSproc(IStoredProcedure sproc, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            var entityType = sproc.EntityType;
            var storeObjectIdentifier = sproc.GetStoreIdentifier();

            if (sproc.ResultColumns.Any())
            {
                throw new InvalidOperationException(
                    H2Strings.StoredProcedureResultColumnsNotSupported(
                        entityType.DisplayName(),
                        storeObjectIdentifier.DisplayName()));
            }

            if (sproc.IsRowsAffectedReturned)
            {
                throw new InvalidOperationException(
                    H2Strings.StoredProcedureReturnValueNotSupported(
                        entityType.DisplayName(),
                        storeObjectIdentifier.DisplayName()));
            }
        }
    }

    /// <inheritdoc />
    protected override void ValidateCompatible(
        IProperty property,
        IProperty duplicateProperty,
        string columnName,
        in StoreObjectIdentifier storeObject,
        IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
    {
    }
}
