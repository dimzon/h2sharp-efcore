using H2Sharp.EntityFrameworkCore.Storage.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Design.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     Upstream this registered the plugin's type mapping source and code generator, both of which
///     existed to serve the ADO-level NodaTime handlers. The conversion now happens in the model, so
///     the design-time service provider needs the convention and nothing else.
/// </remarks>
[UsedImplicitly]
public class H2NodaTimeDesignTimeServices : IDesignTimeServices
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
        => serviceCollection
            .AddSingleton<IRelationalTypeMappingSourcePlugin, H2NodaTimeTypeMappingSourcePlugin>();
}
