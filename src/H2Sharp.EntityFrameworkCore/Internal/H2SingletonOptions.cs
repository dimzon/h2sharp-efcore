using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2SingletonOptions : IH2SingletonOptions
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EntityFramework Core release.
    /// </summary>
    public virtual bool ReverseNullOrderingEnabled { get; private set; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public IReadOnlyList<EnumDefinition> EnumDefinitions { get; private set; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2SingletonOptions()
    {
        EnumDefinitions = [];
    }

    /// <inheritdoc />
    public virtual void Initialize(IDbContextOptions options)
    {
        var npgsqlOptions = options.FindExtension<H2OptionsExtension>() ?? new H2OptionsExtension();

        ReverseNullOrderingEnabled = npgsqlOptions.ReverseNullOrdering;
        EnumDefinitions = npgsqlOptions.EnumDefinitions;
    }

    /// <inheritdoc />
    public virtual void Validate(IDbContextOptions options)
    {
        var npgsqlOptions = options.FindExtension<H2OptionsExtension>() ?? new H2OptionsExtension();

        if (ReverseNullOrderingEnabled != npgsqlOptions.ReverseNullOrdering)
        {
            throw new InvalidOperationException(
                CoreStrings.SingletonOptionChanged(
                    nameof(H2DbContextOptionsBuilder.ReverseNullOrdering),
                    nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
        }

        if (!EnumDefinitions.SequenceEqual(npgsqlOptions.EnumDefinitions))
        {
            throw new InvalidOperationException(
                CoreStrings.SingletonOptionChanged(
                    nameof(H2DbContextOptionsBuilder.MapEnum),
                    nameof(DbContextOptionsBuilder.UseInternalServiceProvider)));
        }

    }
}
