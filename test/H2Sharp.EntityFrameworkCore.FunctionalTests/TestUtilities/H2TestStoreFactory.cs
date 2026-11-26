using System.Collections.Concurrent;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

#nullable enable

public class H2TestStoreFactory(
        string? scriptPath = null,
        string? additionalSql = null,
        string? connectionStringOptions = null,
        bool useConnectionString = false) : RelationalTestStoreFactory
{
    public static H2TestStoreFactory Instance { get; } = new();

    // The ManyToMany query and no-tracking classes share one fixture but run in parallel.
    // Connection-string mode gives each context its own connection, so one class's teardown
    // cannot cancel the session another class is still using.
    public static H2TestStoreFactory ManyToMany { get; } = new(useConnectionString: true);

    // Concurrency fixtures pass an IDbContextTransaction between contexts of the same class, so
    // they need the store's shared connection. Their own stores and databases keep their open
    // transactions away from the other F1 fixtures that use the default store.
    public static H2TestStoreFactory Concurrency { get; } = new();
    public static H2TestStoreFactory ConcurrencyBytes { get; } = new();

    // EF's fixtures share a store by name. The shared store must be created once: H2TestStore's
    // constructor empties its database, so a fresh instance per fixture drops the tables another
    // fixture has just seeded ("Table ... not found (this database is empty)").
    private readonly ConcurrentDictionary<string, TestStore> sharedStores = new(StringComparer.Ordinal);

    public override TestStore Create(string storeName)
        => new H2TestStore(storeName, scriptPath, additionalSql, connectionStringOptions, shared: false, useConnectionString);

    public override TestStore GetOrCreate(string storeName)
        => sharedStores.GetOrAdd(
            storeName,
            name => new H2TestStore(name, scriptPath, additionalSql, connectionStringOptions, shared: true, useConnectionString));

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkH2();
}
