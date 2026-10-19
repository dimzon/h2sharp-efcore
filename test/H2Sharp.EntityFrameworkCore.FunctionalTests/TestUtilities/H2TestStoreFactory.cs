namespace H2Sharp.EntityFrameworkCore.TestUtilities;

#nullable enable

public class H2TestStoreFactory(
        string? scriptPath = null,
        string? additionalSql = null,
        string? connectionStringOptions = null,
        bool useConnectionString = false) : RelationalTestStoreFactory
{
    public static H2TestStoreFactory Instance { get; } = new();

    public override TestStore Create(string storeName)
        => new H2TestStore(storeName, scriptPath, additionalSql, connectionStringOptions, shared: false, useConnectionString);

    public override TestStore GetOrCreate(string storeName)
        => new H2TestStore(storeName, scriptPath, additionalSql, connectionStringOptions, shared: true, useConnectionString);

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkH2();
}
