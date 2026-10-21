using H2Sharp.EntityFrameworkCore.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     H2-specific extension methods for <see cref="EntityTypeBuilder" />.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see>.
/// </remarks>
public static class H2EntityTypeBuilderExtensions
{

    #region Storage parameters

    /// <summary>
    ///     Sets a PostgreSQL storage parameter on the table created for this entity.
    /// </summary>
    /// <remarks>
    ///     See https://www.postgresql.org/docs/current/static/sql-createtable.html#SQL-CREATETABLE-STORAGE-PARAMETERS
    /// </remarks>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="parameterName"> The name of the storage parameter. </param>
    /// <param name="parameterValue"> The value of the storage parameter. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder HasStorageParameter(
        this EntityTypeBuilder entityTypeBuilder,
        string parameterName,
        object? parameterValue)
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

        entityTypeBuilder.Metadata.SetStorageParameter(parameterName, parameterValue);

        return entityTypeBuilder;
    }

    /// <summary>
    ///     Sets a PostgreSQL storage parameter on the table created for this entity.
    /// </summary>
    /// <remarks>
    ///     See https://www.postgresql.org/docs/current/static/sql-createtable.html#SQL-CREATETABLE-STORAGE-PARAMETERS
    /// </remarks>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="parameterName"> The name of the storage parameter. </param>
    /// <param name="parameterValue"> The value of the storage parameter. </param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static EntityTypeBuilder<TEntity> HasStorageParameter<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        string parameterName,
        object? parameterValue)
        where TEntity : class
        => (EntityTypeBuilder<TEntity>)HasStorageParameter((EntityTypeBuilder)entityTypeBuilder, parameterName, parameterValue);

    /// <summary>
    ///     Sets a PostgreSQL storage parameter on the table created for this entity.
    /// </summary>
    /// <remarks>
    ///     See https://www.postgresql.org/docs/current/static/sql-createtable.html#SQL-CREATETABLE-STORAGE-PARAMETERS
    /// </remarks>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="parameterName"> The name of the storage parameter. </param>
    /// <param name="parameterValue"> The value of the storage parameter. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns> The same builder instance so that multiple calls can be chained. </returns>
    public static IConventionEntityTypeBuilder? HasStorageParameter(
        this IConventionEntityTypeBuilder entityTypeBuilder,
        string parameterName,
        object? parameterValue,
        bool fromDataAnnotation = false)
    {
        if (entityTypeBuilder.CanSetStorageParameter(parameterName, parameterValue, fromDataAnnotation))
        {
            entityTypeBuilder.Metadata.SetStorageParameter(parameterName, parameterValue);

            return entityTypeBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the PostgreSQL storage parameter is set on the table created for this entity.
    /// </summary>
    /// <remarks>
    ///     See https://www.postgresql.org/docs/current/static/sql-createtable.html#SQL-CREATETABLE-STORAGE-PARAMETERS
    /// </remarks>
    /// <param name="entityTypeBuilder"> The builder for the entity type being configured. </param>
    /// <param name="parameterName"> The name of the storage parameter. </param>
    /// <param name="parameterValue"> The value of the storage parameter. </param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><c>true</c> if the mapped table can be configured as with the storage parameter.</returns>
    public static bool CanSetStorageParameter(
        this IConventionEntityTypeBuilder entityTypeBuilder,
        string parameterName,
        object? parameterValue,
        bool fromDataAnnotation = false)
    {
        Check.NotNull(entityTypeBuilder, nameof(entityTypeBuilder));

        return entityTypeBuilder.CanSetAnnotation(
            H2AnnotationNames.StorageParameterPrefix + parameterName, parameterValue, fromDataAnnotation);
    }

    #endregion Storage parameters
}
