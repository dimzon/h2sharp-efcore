using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Internal;

/// <inheritdoc />
public class H2NetTopologySuiteSingletonOptions : IH2NetTopologySuiteSingletonOptions
{
    /// <inheritdoc />
    public virtual CoordinateSequenceFactory? CoordinateSequenceFactory { get; set; }

    /// <inheritdoc />
    public virtual PrecisionModel? PrecisionModel { get; set; }

    /// <inheritdoc />
    public virtual Ordinates HandleOrdinates { get; set; }

    /// <inheritdoc />
    public virtual bool IsGeographyDefault { get; set; }

    /// <inheritdoc />
    public virtual void Initialize(IDbContextOptions options)
    {
        var npgsqlNtsOptions = options.FindExtension<H2NetTopologySuiteOptionsExtension>()
            ?? new H2NetTopologySuiteOptionsExtension();

        CoordinateSequenceFactory = npgsqlNtsOptions.CoordinateSequenceFactory;
        PrecisionModel = npgsqlNtsOptions.PrecisionModel;
        HandleOrdinates = npgsqlNtsOptions.HandleOrdinates;
    }

    /// <inheritdoc />
    public virtual void Validate(IDbContextOptions options) { }
}
