using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Extension methods for <see cref="IIndex" /> for H2-specific metadata.
/// </summary>
public static class H2IndexExtensions
{
    #region Method

    /// <summary>
    ///     Returns the index method to be used, or <c>null</c> if it hasn't been specified.
    ///     <c>null</c> selects the default (currently <c>btree</c>).
    /// </summary>
    /// <remarks>
    ///     http://www.postgresql.org/docs/current/static/sql-createindex.html
    /// </remarks>
    public static string? GetMethod(this IReadOnlyIndex index)
        => (string?)index[H2AnnotationNames.IndexMethod];

    /// <summary>
    ///     Sets the index method to be used, or <c>null</c> if it hasn't been specified.
    ///     <c>null</c> selects the default (currently <c>btree</c>).
    /// </summary>
    /// <remarks>
    ///     http://www.postgresql.org/docs/current/static/sql-createindex.html
    /// </remarks>
    public static void SetMethod(this IMutableIndex index, string? method)
        => index.SetOrRemoveAnnotation(H2AnnotationNames.IndexMethod, method);

    /// <summary>
    ///     Sets the index method to be used, or <c>null</c> if it hasn't been specified.
    ///     <c>null</c> selects the default (currently <c>btree</c>).
    /// </summary>
    /// <remarks>
    ///     http://www.postgresql.org/docs/current/static/sql-createindex.html
    /// </remarks>
    public static string? SetMethod(
        this IConventionIndex index,
        string? method,
        bool fromDataAnnotation = false)
    {
        Check.NullButNotEmpty(method, nameof(method));

        index.SetOrRemoveAnnotation(H2AnnotationNames.IndexMethod, method, fromDataAnnotation);

        return method;
    }

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for the index method.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the index method.</returns>
    public static ConfigurationSource? GetMethodConfigurationSource(this IConventionIndex index)
        => index.FindAnnotation(H2AnnotationNames.IndexMethod)?.GetConfigurationSource();

    #endregion Method



    #region Null sort order

    /// <summary>
    ///     Returns the column NULL sort orders to be used, or <c>null</c> if they have not been specified.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/indexes-ordering.html
    /// </remarks>
    public static IReadOnlyList<NullSortOrder>? GetNullSortOrder(this IReadOnlyIndex index)
        => (IReadOnlyList<NullSortOrder>?)index[H2AnnotationNames.IndexNullSortOrder];

    /// <summary>
    ///     Sets the column NULL sort orders to be used, or <c>null</c> if they have not been specified.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/indexes-ordering.html
    /// </remarks>
    public static void SetNullSortOrder(this IMutableIndex index, IReadOnlyList<NullSortOrder>? nullSortOrder)
        => index.SetOrRemoveAnnotation(H2AnnotationNames.IndexNullSortOrder, nullSortOrder);

    /// <summary>
    ///     Sets the column NULL sort orders to be used, or <c>null</c> if they have not been specified.
    /// </summary>
    /// <remarks>
    ///     https://www.postgresql.org/docs/current/static/indexes-ordering.html
    /// </remarks>
    public static IReadOnlyList<NullSortOrder>? SetNullSortOrder(
        this IConventionIndex index,
        IReadOnlyList<NullSortOrder>? nullSortOrder,
        bool fromDataAnnotation = false)
    {
        Check.NullButNotEmpty(nullSortOrder, nameof(nullSortOrder));

        index.SetOrRemoveAnnotation(H2AnnotationNames.IndexNullSortOrder, nullSortOrder, fromDataAnnotation);

        return nullSortOrder;
    }

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for the index null sort orders.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the index null sort orders.</returns>
    public static ConfigurationSource? GetNullSortOrderConfigurationSource(this IConventionIndex index)
        => index.FindAnnotation(H2AnnotationNames.IndexNullSortOrder)?.GetConfigurationSource();

    #endregion

    #region Included properties

    /// <summary>
    ///     Returns included property names, or <c>null</c> if they have not been specified.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The included property names, or <c>null</c> if they have not been specified.</returns>
    public static IReadOnlyList<string>? GetIncludeProperties(this IReadOnlyIndex index)
        => (IReadOnlyList<string>?)index[H2AnnotationNames.IndexInclude];

    /// <summary>
    ///     Sets included property names.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="properties">The value to set.</param>
    public static void SetIncludeProperties(this IMutableIndex index, IReadOnlyList<string>? properties)
        => index.SetOrRemoveAnnotation(
            H2AnnotationNames.IndexInclude,
            properties);

    /// <summary>
    ///     Sets included property names.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <param name="properties">The value to set.</param>
    public static IReadOnlyList<string>? SetIncludeProperties(
        this IConventionIndex index,
        IReadOnlyList<string>? properties,
        bool fromDataAnnotation = false)
    {
        Check.NullButNotEmpty(properties, nameof(properties));

        index.SetOrRemoveAnnotation(
            H2AnnotationNames.IndexInclude,
            properties,
            fromDataAnnotation);

        return properties;
    }

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for the included property names.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the included property names.</returns>
    public static ConfigurationSource? GetIncludePropertiesConfigurationSource(this IConventionIndex index)
        => index.FindAnnotation(H2AnnotationNames.IndexInclude)?.GetConfigurationSource();

    #endregion Included properties


    #region NULLS distinct

    /// <summary>
    ///     Returns whether for a unique index, null values should be considered distinct (not equal).
    ///     The default is that they are distinct, so that a unique index could contain multiple null values in a column.
    /// </summary>
    /// <remarks>
    ///     http://www.postgresql.org/docs/current/static/sql-createindex.html
    /// </remarks>
    public static bool? GetAreNullsDistinct(this IReadOnlyIndex index)
        => (bool?)index[H2AnnotationNames.NullsDistinct];

    /// <summary>
    ///     Sets whether for a unique index, null values should be considered distinct (not equal).
    ///     The default is that they are distinct, so that a unique index could contain multiple null values in a column.
    /// </summary>
    /// <remarks>
    ///     http://www.postgresql.org/docs/current/static/sql-createindex.html
    /// </remarks>
    public static void SetAreNullsDistinct(this IMutableIndex index, bool? nullsDistinct)
        => index.SetOrRemoveAnnotation(H2AnnotationNames.NullsDistinct, nullsDistinct);

    /// <summary>
    ///     Sets whether for a unique index, null values should be considered distinct (not equal).
    ///     The default is that they are distinct, so that a unique index could contain multiple null values in a column.
    /// </summary>
    /// <remarks>
    ///     http://www.postgresql.org/docs/current/static/sql-createindex.html
    /// </remarks>
    public static bool? SetAreNullsDistinct(this IConventionIndex index, bool? nullsDistinct, bool fromDataAnnotation = false)
    {
        index.SetOrRemoveAnnotation(H2AnnotationNames.NullsDistinct, nullsDistinct, fromDataAnnotation);

        return nullsDistinct;
    }

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for whether nulls are considered distinct.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The <see cref="ConfigurationSource" />.</returns>
    public static ConfigurationSource? GetAreNullsDistinctConfigurationSource(this IConventionIndex index)
        => index.FindAnnotation(H2AnnotationNames.NullsDistinct)?.GetConfigurationSource();

    #endregion NULLS distinct


}
