using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2CompiledQueryCacheKeyGenerator : RelationalCompiledQueryCacheKeyGenerator
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2CompiledQueryCacheKeyGenerator(
        CompiledQueryCacheKeyGeneratorDependencies dependencies,
        RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override object GenerateCacheKey(Expression query, bool async)
        => new H2CompiledQueryCacheKey(
            GenerateCacheKeyCore(query, async),
            RelationalDependencies.ContextOptions.FindExtension<H2OptionsExtension>()?.ReverseNullOrdering ?? false);

    private struct H2CompiledQueryCacheKey(
        RelationalCompiledQueryCacheKey relationalCompiledQueryCacheKey,
        bool reverseNullOrdering)
    {
        private readonly RelationalCompiledQueryCacheKey _relationalCompiledQueryCacheKey = relationalCompiledQueryCacheKey;
        private readonly bool _reverseNullOrdering = reverseNullOrdering;

        public override bool Equals(object? obj)
            => !(obj is null)
                && obj is H2CompiledQueryCacheKey key
                && Equals(key);

        private bool Equals(H2CompiledQueryCacheKey other)
            => _relationalCompiledQueryCacheKey.Equals(other._relationalCompiledQueryCacheKey)
                && _reverseNullOrdering == other._reverseNullOrdering;

        public override int GetHashCode()
            => HashCode.Combine(_relationalCompiledQueryCacheKey, _reverseNullOrdering);
    }
}
