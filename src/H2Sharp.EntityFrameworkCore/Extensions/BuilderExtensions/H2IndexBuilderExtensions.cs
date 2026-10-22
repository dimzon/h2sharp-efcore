using System.Collections;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     H2 specific extension methods for <see cref="IndexBuilder" />.
/// </summary>
public static class H2IndexBuilderExtensions
{
    #region Method

    /// <summary>
    ///     The kind of the index to be used. Null or <c>btree</c> selects the default index of H2 2.5,
    ///     <c>spatial</c> selects its spatial index.
    /// </summary>
    /// <remarks>
    ///     H2 2.5 knows the default index and the spatial index. Its hash index is read only in the
    ///     compatibility mode, and the index methods of PostgreSQL (<c>gin</c>, <c>gist</c>, <c>brin</c>)
    ///     are not in H2 2.5 at all.
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="method">The kind of the index.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder HasMethod(
        this IndexBuilder indexBuilder,
        string? method)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));
        Check.NullButNotEmpty(method, nameof(method));

        indexBuilder.Metadata.SetMethod(CheckIndexMethod(method));

        return indexBuilder;
    }

    /// <summary>
    ///     The kind of the index to be used. Null or <c>btree</c> selects the default index of H2 2.5,
    ///     <c>spatial</c> selects its spatial index.
    /// </summary>
    /// <remarks>
    ///     H2 2.5 knows the default index and the spatial index. Its hash index is read only in the
    ///     compatibility mode, and the index methods of PostgreSQL (<c>gin</c>, <c>gist</c>, <c>brin</c>)
    ///     are not in H2 2.5 at all.
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="method">The kind of the index.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder<TEntity> HasMethod<TEntity>(
        this IndexBuilder<TEntity> indexBuilder,
        string? method)
        => (IndexBuilder<TEntity>)HasMethod((IndexBuilder)indexBuilder, method);

    /// <summary>
    ///     The kind of the index to be used. Null or <c>btree</c> selects the default index of H2 2.5,
    ///     <c>spatial</c> selects its spatial index.
    /// </summary>
    /// <remarks>
    ///     H2 2.5 knows the default index and the spatial index. Its hash index is read only in the
    ///     compatibility mode, and the index methods of PostgreSQL (<c>gin</c>, <c>gist</c>, <c>brin</c>)
    ///     are not in H2 2.5 at all.
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="method">The kind of the index.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IConventionIndexBuilder? HasMethod(
        this IConventionIndexBuilder indexBuilder,
        string? method,
        bool fromDataAnnotation = false)
    {
        if (indexBuilder.CanSetMethod(method, fromDataAnnotation))
        {
            indexBuilder.Metadata.SetMethod(CheckIndexMethod(method), fromDataAnnotation);

            return indexBuilder;
        }

        return null;
    }

    /// <summary>
    ///     The kind of the index to be used. Null or <c>btree</c> selects the default index of H2 2.5,
    ///     <c>spatial</c> selects its spatial index.
    /// </summary>
    /// <remarks>
    ///     H2 2.5 knows the default index and the spatial index. Its hash index is read only in the
    ///     compatibility mode, and the index methods of PostgreSQL (<c>gin</c>, <c>gist</c>, <c>brin</c>)
    ///     are not in H2 2.5 at all.
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="method">The kind of the index.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><c>true</c> if the index can be configured with the method</returns>
    public static bool CanSetMethod(
        this IConventionIndexBuilder indexBuilder,
        string? method,
        bool fromDataAnnotation = false)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));

        return indexBuilder.CanSetAnnotation(H2AnnotationNames.IndexMethod, method, fromDataAnnotation);
    }

    /// <summary>
    ///     Checks that the kind of the index is one H2 2.5 has.
    /// </summary>
    /// <param name="method">The kind of the index.</param>
    /// <returns>The kind of the index.</returns>
    /// <exception cref="NotSupportedException">
    ///     H2 2.5 has no index of that kind.
    /// </exception>
    private static string? CheckIndexMethod(string? method)
    {
        if (method is null || method is "btree" or "spatial")
        {
            return method;
        }

        throw new NotSupportedException(
            $"H2 2.5 has no index of the kind '{method}': the kinds of the index of H2 2.5 are the default "
            + "index (btree) and the spatial index.");
    }

    #endregion Method




    #region Null sort order

    /// <summary>
    ///     The PostgreSQL index NULL sort ordering to be used.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/indexes-ordering.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="values">The sort order to use for each column.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder HasNullSortOrder(
        this IndexBuilder indexBuilder,
        params NullSortOrder[]? values)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));
        Check.NullButNotEmpty(values, nameof(values));

        if (!SortOrderHelper.IsDefaultNullSortOrder(values, indexBuilder.Metadata.IsDescending))
        {
            indexBuilder.Metadata.SetNullSortOrder(values);
        }

        return indexBuilder;
    }

    /// <summary>
    ///     The PostgreSQL index NULL sort ordering to be used.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/indexes-ordering.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="values">The sort order to use for each column.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder<TEntity> HasNullSortOrder<TEntity>(
        this IndexBuilder<TEntity> indexBuilder,
        params NullSortOrder[]? values)
        => (IndexBuilder<TEntity>)HasNullSortOrder((IndexBuilder)indexBuilder, values);

    /// <summary>
    ///     The PostgreSQL index NULL sort ordering to be used.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/indexes-ordering.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="values">The sort order to use for each column.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IConventionIndexBuilder? HasNullSortOrder(
        this IConventionIndexBuilder indexBuilder,
        IReadOnlyList<NullSortOrder>? values,
        bool fromDataAnnotation)
    {
        if (indexBuilder.CanSetNullSortOrder(values, fromDataAnnotation))
        {
            if (!SortOrderHelper.IsDefaultNullSortOrder(values, indexBuilder.Metadata.IsDescending))
            {
                indexBuilder.Metadata.SetNullSortOrder(values, fromDataAnnotation);
            }

            return indexBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the PostgreSQL index null sort ordering can be set.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/indexes-ordering.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="values">The sort order to use for each column.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static bool CanSetNullSortOrder(
        this IConventionIndexBuilder indexBuilder,
        IReadOnlyList<NullSortOrder>? values,
        bool fromDataAnnotation)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));

        return indexBuilder.CanSetAnnotation(H2AnnotationNames.IndexNullSortOrder, values, fromDataAnnotation);
    }

    #endregion Null sort order

    #region Include

    /// <summary>
    ///     Adds an INCLUDE clause to the index definition with the specified property names.
    ///     This clause specifies a list of columns which will be included as a non-key part in the index.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="propertyNames">An array of property names to be used in INCLUDE clause.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder IncludeProperties(
        this IndexBuilder indexBuilder,
        params string[] propertyNames)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));
        Check.NullButNotEmpty(propertyNames, nameof(propertyNames));

        indexBuilder.Metadata.SetIncludeProperties(propertyNames);

        return indexBuilder;
    }

    /// <summary>
    ///     Adds an INCLUDE clause to the index definition with the specified property names.
    ///     This clause specifies a list of columns which will be included as a non-key part in the index.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="propertyNames">An array of property names to be used in INCLUDE clause.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder<TEntity> IncludeProperties<TEntity>(
        this IndexBuilder<TEntity> indexBuilder,
        params string[] propertyNames)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));
        Check.NullButNotEmpty(propertyNames, nameof(propertyNames));

        indexBuilder.Metadata.SetIncludeProperties(propertyNames);

        return indexBuilder;
    }

    /// <summary>
    ///     Adds an INCLUDE clause to the index definition with property names from the specified expression.
    ///     This clause specifies a list of columns which will be included as a non-key part in the index.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="includeExpression">
    ///     <para>
    ///         A lambda expression representing the property(s) to be included in the INCLUDE clause
    ///         (<c>blog => blog.Url</c>).
    ///     </para>
    ///     <para>
    ///         If multiple properties are to be included then specify an anonymous type including the
    ///         properties (<c>post => new { post.Title, post.BlogId }</c>).
    ///     </para>
    /// </param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder<TEntity> IncludeProperties<TEntity>(
        this IndexBuilder<TEntity> indexBuilder,
        Expression<Func<TEntity, object>> includeExpression)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));
        Check.NotNull(includeExpression, nameof(includeExpression));

        indexBuilder.IncludeProperties(includeExpression.GetPropertyAccessList().Select(x => x.Name).ToArray());

        return indexBuilder;
    }

    /// <summary>
    ///     Adds an INCLUDE clause to the index definition with the specified property names.
    ///     This clause specifies a list of columns which will be included as a non-key part in the index.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="propertyNames">An array of property names to be used in INCLUDE clause.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IConventionIndexBuilder? IncludeProperties(
        this IConventionIndexBuilder indexBuilder,
        IReadOnlyList<string> propertyNames,
        bool fromDataAnnotation = false)
    {
        if (indexBuilder.CanSetIncludeProperties(propertyNames, fromDataAnnotation))
        {
            indexBuilder.Metadata.SetIncludeProperties(propertyNames, fromDataAnnotation);

            return indexBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the given include properties can be set.
    /// </summary>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="propertyNames">An array of property names to be used in 'include' clause.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns> <c>true</c> if the given include properties can be set. </returns>
    public static bool CanSetIncludeProperties(
        this IConventionIndexBuilder indexBuilder,
        IReadOnlyList<string>? propertyNames,
        bool fromDataAnnotation = false)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));

        return (fromDataAnnotation ? ConfigurationSource.DataAnnotation : ConfigurationSource.Convention)
            .Overrides(indexBuilder.Metadata.GetIncludePropertiesConfigurationSource())
            || StructuralComparisons.StructuralEqualityComparer.Equals(
                propertyNames, indexBuilder.Metadata.GetIncludeProperties());
    }

    #endregion Include


    #region NULLS distinct

    /// <summary>
    ///     Specifies whether for a unique index, null values should be considered distinct (not equal).
    ///     The default is that they are distinct, so that a unique index could contain multiple null values in a column.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="nullsDistinct">Whether nulls should be considered distinct.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder AreNullsDistinct(
        this IndexBuilder indexBuilder,
        bool nullsDistinct = true)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));

        indexBuilder.Metadata.SetAreNullsDistinct(nullsDistinct);

        return indexBuilder;
    }

    /// <summary>
    ///     Specifies whether for a unique index, null values should be considered distinct (not equal).
    ///     The default is that they are distinct, so that a unique index could contain multiple null values in a column.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="nullsDistinct">Whether nulls should be considered distinct.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IndexBuilder<TEntity> AreNullsDistinct<TEntity>(
        this IndexBuilder<TEntity> indexBuilder,
        bool nullsDistinct = true)
        => (IndexBuilder<TEntity>)AreNullsDistinct((IndexBuilder)indexBuilder, nullsDistinct);

    /// <summary>
    ///     Specifies whether for a unique index, null values should be considered distinct (not equal).
    ///     The default is that they are distinct, so that a unique index could contain multiple null values in a column.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="nullsDistinct">Whether nulls should be considered distinct.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>A builder to further configure the index.</returns>
    public static IConventionIndexBuilder? AreNullsDistinct(
        this IConventionIndexBuilder indexBuilder,
        bool nullsDistinct = true,
        bool fromDataAnnotation = false)
    {
        if (indexBuilder.CanSetAreNullsDistinct(nullsDistinct, fromDataAnnotation))
        {
            indexBuilder.Metadata.SetAreNullsDistinct(nullsDistinct, fromDataAnnotation);

            return indexBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Specifies whether for a unique index, null values should be considered distinct (not equal).
    ///     The default is that they are distinct, so that a unique index could contain multiple null values in a column.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/sql-createindex.html
    /// </remarks>
    /// <param name="indexBuilder">The builder for the index being configured.</param>
    /// <param name="nullsDistinct">Whether nulls should be considered distinct.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><c>true</c> if the index can be configured with the method</returns>
    public static bool CanSetAreNullsDistinct(
        this IConventionIndexBuilder indexBuilder,
        bool nullsDistinct = true,
        bool fromDataAnnotation = false)
    {
        Check.NotNull(indexBuilder, nameof(indexBuilder));

        return indexBuilder.CanSetAnnotation(H2AnnotationNames.NullsDistinct, nullsDistinct, fromDataAnnotation);
    }

    #endregion NULLS distinct

}
