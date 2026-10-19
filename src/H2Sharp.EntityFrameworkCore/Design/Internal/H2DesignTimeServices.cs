using Microsoft.EntityFrameworkCore.Design.Internal;
using H2Sharp.EntityFrameworkCore.Scaffolding.Internal;

namespace H2Sharp.EntityFrameworkCore.Design.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2DesignTimeServices : IDesignTimeServices
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        Check.NotNull(serviceCollection, nameof(serviceCollection));

        serviceCollection.AddEntityFrameworkH2();
#pragma warning disable EF1001 // Internal EF Core API usage.
        new EntityFrameworkRelationalDesignServicesBuilder(serviceCollection)
            .TryAdd<ICSharpRuntimeAnnotationCodeGenerator, H2CSharpRuntimeAnnotationCodeGenerator>()
#pragma warning restore EF1001 // Internal EF Core API usage.
            .TryAdd<IAnnotationCodeGenerator, H2AnnotationCodeGenerator>()
            .TryAdd<IDatabaseModelFactory, H2DatabaseModelFactory>()
            .TryAdd<IProviderConfigurationCodeGenerator, H2CodeGenerator>()
            .TryAddCoreServices();
    }
}
