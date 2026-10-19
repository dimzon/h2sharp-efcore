using H2Sharp.EntityFrameworkCore.Diagnostics.Internal;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class H2TestHelpers : RelationalTestHelpers
{
    protected H2TestHelpers() { }

    public static H2TestHelpers Instance { get; } = new();

    public override IServiceCollection AddProviderServices(IServiceCollection services)
        => services.AddEntityFrameworkH2();

    public override DbContextOptionsBuilder UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseH2(new H2Connection("Host=localhost;Database=DummyDatabase"));

    public override LoggingDefinitions LoggingDefinitions { get; } = new H2LoggingDefinitions();
}
