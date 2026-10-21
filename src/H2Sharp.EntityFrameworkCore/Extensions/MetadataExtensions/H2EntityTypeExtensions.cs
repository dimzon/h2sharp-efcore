using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Extension methods for <see cref="IEntityType" /> for H2-specific metadata.
/// </summary>
public static class H2EntityTypeExtensions
{
    #region Storage parameters

    /// <summary>
    ///     Gets all storage parameters for the table mapped to the entity type.
    /// </summary>
    public static Dictionary<string, object?> GetStorageParameters(this IReadOnlyEntityType entityType)
        => entityType.GetAnnotations()
            .Where(a => a.Name.StartsWith(H2AnnotationNames.StorageParameterPrefix, StringComparison.Ordinal))
            .ToDictionary(
                a => a.Name.Substring(H2AnnotationNames.StorageParameterPrefix.Length),
                a => a.Value);

    /// <summary>
    ///     Gets a storage parameter for the table mapped to the entity type.
    /// </summary>
    public static string? GetStorageParameter(this IEntityType entityType, string parameterName)
    {
        Check.NotEmpty(parameterName, nameof(parameterName));

        return (string?)entityType[H2AnnotationNames.StorageParameterPrefix + parameterName];
    }

    /// <summary>
    ///     Sets a storage parameter on the table mapped to the entity type.
    /// </summary>
    public static void SetStorageParameter(this IMutableEntityType entityType, string parameterName, object? parameterValue)
    {
        Check.NotEmpty(parameterName, nameof(parameterName));

        entityType.SetOrRemoveAnnotation(H2AnnotationNames.StorageParameterPrefix + parameterName, parameterValue);
    }

    /// <summary>
    ///     Sets a storage parameter on the table mapped to the entity type.
    /// </summary>
    public static object SetStorageParameter(
        this IConventionEntityType entityType,
        string parameterName,
        object? parameterValue,
        bool fromDataAnnotation = false)
    {
        Check.NotEmpty(parameterName, nameof(parameterName));

        entityType.SetOrRemoveAnnotation(H2AnnotationNames.StorageParameterPrefix + parameterName, parameterValue, fromDataAnnotation);

        return parameterName;
    }

    /// <summary>
    ///     Gets the configuration source for a storage parameter for the table mapped to the entity type.
    /// </summary>
    public static ConfigurationSource? GetStorageParameterConfigurationSource(
        this IConventionEntityType index,
        string parameterName)
    {
        Check.NotEmpty(parameterName, nameof(parameterName));

        return index.FindAnnotation(H2AnnotationNames.StorageParameterPrefix + parameterName)?.GetConfigurationSource();
    }

    #endregion Storage parameters
}
