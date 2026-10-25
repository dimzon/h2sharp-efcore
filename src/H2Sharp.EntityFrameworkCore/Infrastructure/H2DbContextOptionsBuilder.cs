using System.Net.Security;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore.Infrastructure;

/// <summary>
///     Allows for options specific to H2 to be configured for a <see cref="DbContext" />.
/// </summary>
public class H2DbContextOptionsBuilder
    : RelationalDbContextOptionsBuilder<H2DbContextOptionsBuilder, H2OptionsExtension>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="H2DbContextOptionsBuilder" /> class.
    /// </summary>
    /// <param name="optionsBuilder"> The core options builder.</param>
    public H2DbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        : base(optionsBuilder)
    {
    }


    #region MapEnum

    /// <summary>
    ///     Maps an H2 enum type for use.
    /// </summary>
    /// <param name="enumName">The name of the H2 enum type to be mapped.</param>
    /// <param name="schemaName">The name of the schema in which the enum type is defined.</param>
    /// <param name="nameTranslator">The name translator used to map enum value names to H2 enum values.</param>
    public virtual H2DbContextOptionsBuilder MapEnum<T>(
        string? enumName = null,
        string? schemaName = null,
        IH2NameTranslator? nameTranslator = null)
        where T : struct, Enum
        => MapEnum(typeof(T), enumName, schemaName, nameTranslator);

    /// <summary>
    ///     Maps an H2 enum type for use.
    /// </summary>
    /// <param name="clrType">The CLR type of the enum.</param>
    /// <param name="enumName">The name of the H2 enum type to be mapped.</param>
    /// <param name="schemaName">The name of the schema in which the enum type is defined.</param>
    /// <param name="nameTranslator">The name translator used to map enum value names to H2 enum values.</param>
    public virtual H2DbContextOptionsBuilder MapEnum(
        Type clrType,
        string? enumName = null,
        string? schemaName = null,
        IH2NameTranslator? nameTranslator = null)
        => WithOption(e => e.WithEnumMapping(clrType, enumName, schemaName, nameTranslator));

    #endregion MapEnum

    /// <summary>
    ///     Appends NULLS FIRST to all ORDER BY clauses. This is important for the tests which were written
    ///     for SQL Server. Note that to fully implement null-first ordering indexes also need to be generated
    ///     accordingly, and since this isn't done this feature isn't publicly exposed.
    /// </summary>
    /// <param name="reverseNullOrdering">True to enable reverse null ordering; otherwise, false.</param>
    internal virtual H2DbContextOptionsBuilder ReverseNullOrdering(bool reverseNullOrdering = true)
        => WithOption(e => e.WithReverseNullOrdering(reverseNullOrdering));

    #region Authentication (obsolete)

    #endregion Authentication (obsolete)

    #region Retrying execution strategy

    /// <summary>
    ///     Configures the context to use the default retrying <see cref="IExecutionStrategy" />.
    /// </summary>
    /// <returns>
    ///     An instance of <see cref="H2DbContextOptionsBuilder" /> configured to use
    ///     the default retrying <see cref="IExecutionStrategy" />.
    /// </returns>
    public virtual H2DbContextOptionsBuilder EnableRetryOnFailure()
        => ExecutionStrategy(c => new H2RetryingExecutionStrategy(c));

    /// <summary>
    ///     Configures the context to use the default retrying <see cref="IExecutionStrategy" />.
    /// </summary>
    /// <returns>
    ///     An instance of <see cref="H2DbContextOptionsBuilder" /> with the specified parameters.
    /// </returns>
    public virtual H2DbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount)
        => ExecutionStrategy(c => new H2RetryingExecutionStrategy(c, maxRetryCount));

    /// <summary>
    ///     Configures the context to use the default retrying <see cref="IExecutionStrategy" />.
    /// </summary>
    /// <param name="errorCodesToAdd">Additional error codes that should be considered transient.</param>
    /// <returns>
    ///     An instance of <see cref="H2DbContextOptionsBuilder" /> with the specified parameters.
    /// </returns>
    public virtual H2DbContextOptionsBuilder EnableRetryOnFailure(ICollection<string>? errorCodesToAdd)
        => ExecutionStrategy(c => new H2RetryingExecutionStrategy(c, errorCodesToAdd));

    /// <summary>
    ///     Configures the context to use the default retrying <see cref="IExecutionStrategy" />.
    /// </summary>
    /// <param name="maxRetryCount">The maximum number of retry attempts.</param>
    /// <param name="maxRetryDelay">The maximum delay between retries.</param>
    /// <param name="errorCodesToAdd">Additional error codes that should be considered transient.</param>
    /// <returns>
    ///     An instance of <see cref="H2DbContextOptionsBuilder" /> with the specified parameters.
    /// </returns>
    public virtual H2DbContextOptionsBuilder EnableRetryOnFailure(
        int maxRetryCount,
        TimeSpan maxRetryDelay,
        ICollection<string>? errorCodesToAdd)
        => ExecutionStrategy(c => new H2RetryingExecutionStrategy(c, maxRetryCount, maxRetryDelay, errorCodesToAdd));

    #endregion Retrying execution strategy
}
