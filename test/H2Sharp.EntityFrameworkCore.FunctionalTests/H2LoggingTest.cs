using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using H2Sharp.EntityFrameworkCore.Diagnostics.Internal;
using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

#nullable enable

namespace H2Sharp.EntityFrameworkCore;

public class H2LoggingTest : LoggingRelationalTestBase<H2DbContextOptionsBuilder, H2OptionsExtension>
{
    protected override DbContextOptionsBuilder CreateOptionsBuilder(
        IServiceCollection services,
        Action<RelationalDbContextOptionsBuilder<H2DbContextOptionsBuilder, H2OptionsExtension>>? relationalAction)
        => new DbContextOptionsBuilder()
            .UseInternalServiceProvider(services.AddEntityFrameworkH2().BuildServiceProvider())
            .UseH2("Data Source=LoggingH2Test;Storage=Memory", relationalAction);

    protected override TestLogger CreateTestLogger()
        => new TestLogger<H2LoggingDefinitions>();

    protected override string ProviderName
        => "H2Sharp.EntityFrameworkCore";

    protected override string ProviderVersion
        => typeof(H2OptionsExtension).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion!;
}
