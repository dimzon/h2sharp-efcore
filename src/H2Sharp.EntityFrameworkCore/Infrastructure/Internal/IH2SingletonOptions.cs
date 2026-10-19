namespace H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

/// <summary>
///     Represents options for H2 that can only be set at the <see cref="IServiceProvider" /> singleton level.
/// </summary>
public interface IH2SingletonOptions : ISingletonOptions
{
    /// <summary>
    ///     The backend version to target.
    /// </summary>
    /// <summary>
    ///     Whether the user has explicitly set the backend version to target.
    /// </summary>
    /// <summary>
    ///     Whether to target Redshift.
    /// </summary>
    /// <summary>
    ///     Whether reverse null ordering is enabled.
    /// </summary>
    bool ReverseNullOrderingEnabled { get; }

    /// <summary>
    ///     The collection of enum mappings.
    /// </summary>
    IReadOnlyList<EnumDefinition> EnumDefinitions { get; }
}
