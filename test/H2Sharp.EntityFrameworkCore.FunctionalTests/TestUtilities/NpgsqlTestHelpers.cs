using H2Sharp.EntityFrameworkCore.Diagnostics.Internal;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class NpgsqlTestHelpers : RelationalTestHelpers
{
    protected NpgsqlTestHelpers() { }

    public static NpgsqlTestHelpers Instance { get; } = new();

    public override IServiceCollection AddProviderServices(IServiceCollection services)
        => services.AddEntityFrameworkH2();

    public override DbContextOptionsBuilder UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseH2(new NpgsqlConnection("Host=localhost;Database=DummyDatabase"));

    public override LoggingDefinitions LoggingDefinitions { get; } = new NpgsqlLoggingDefinitions();
}
