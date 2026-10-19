using System.Text;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2NetTopologySuiteOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual CoordinateSequenceFactory? CoordinateSequenceFactory { get; private set; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual PrecisionModel? PrecisionModel { get; private set; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Ordinates HandleOrdinates { get; private set; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual bool IsGeographyDefault { get; private set; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2NetTopologySuiteOptionsExtension() { }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2NetTopologySuiteOptionsExtension(H2NetTopologySuiteOptionsExtension copyFrom)
    {
        IsGeographyDefault = copyFrom.IsGeographyDefault;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual H2NetTopologySuiteOptionsExtension Clone()
        => new(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkH2NetTopologySuite();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual H2NetTopologySuiteOptionsExtension WithCoordinateSequenceFactory(
        CoordinateSequenceFactory? coordinateSequenceFactory)
    {
        var clone = Clone();

        clone.CoordinateSequenceFactory = coordinateSequenceFactory;

        return clone;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual H2NetTopologySuiteOptionsExtension WithPrecisionModel(PrecisionModel? precisionModel)
    {
        var clone = Clone();

        clone.PrecisionModel = precisionModel;

        return clone;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual H2NetTopologySuiteOptionsExtension WithHandleOrdinates(Ordinates handleOrdinates)
    {
        var clone = Clone();

        clone.HandleOrdinates = handleOrdinates;

        return clone;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual H2NetTopologySuiteOptionsExtension WithGeographyDefault(bool isGeographyDefault = true)
    {
        var clone = Clone();

        clone.IsGeographyDefault = isGeographyDefault;

        return clone;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void Validate(IDbContextOptions options)
    {
        Check.NotNull(options, nameof(options));

        var internalServiceProvider = options.FindExtension<CoreOptionsExtension>()?.InternalServiceProvider;
        if (internalServiceProvider is not null)
        {
            using (var scope = internalServiceProvider.CreateScope())
            {
                if (scope.ServiceProvider.GetService<IEnumerable<IRelationalTypeMappingSourcePlugin>>()
                        ?.Any(s => s is H2NetTopologySuiteTypeMappingSourcePlugin)
                    != true)
                {
                    throw new InvalidOperationException(
                        $"{nameof(H2NetTopologySuiteDbContextOptionsBuilderExtensions.UseNetTopologySuite)} requires {nameof(H2NetTopologySuiteServiceCollectionExtensions.AddEntityFrameworkH2NetTopologySuite)} to be called on the internal service provider used.");
                }
            }
        }
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        private string? _logFragment;

        private new H2NetTopologySuiteOptionsExtension Extension
            => (H2NetTopologySuiteOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider
            => false;

        public override int GetServiceProviderHashCode()
            => Extension.IsGeographyDefault.GetHashCode();

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo otherInfo
                && ReferenceEquals(Extension.CoordinateSequenceFactory, otherInfo.Extension.CoordinateSequenceFactory)
                && ReferenceEquals(Extension.PrecisionModel, otherInfo.Extension.PrecisionModel)
                && Extension.HandleOrdinates == otherInfo.Extension.HandleOrdinates
                && Extension.IsGeographyDefault == otherInfo.Extension.IsGeographyDefault;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            Check.NotNull(debugInfo, nameof(debugInfo));

            var prefix = "H2:" + nameof(H2NetTopologySuiteDbContextOptionsBuilderExtensions.UseNetTopologySuite);
            debugInfo[prefix] = "1";
            debugInfo[$"{prefix}:{nameof(IsGeographyDefault)}"] = Extension.IsGeographyDefault.ToString();
        }

        public override string LogFragment
        {
            get
            {
                if (_logFragment is null)
                {
                    var builder = new StringBuilder("using NetTopologySuite");
                    if (Extension.IsGeographyDefault)
                    {
                        builder.Append(" (geography by default)");
                    }

                    builder.Append(' ');

                    _logFragment = builder.ToString();
                }

                return _logFragment;
            }
        }
    }
}
