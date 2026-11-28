namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class H2NorthwindTestStoreFactory : H2TestStoreFactory
{
    public const string Name = "Northwind";
    public static readonly string NorthwindConnectionString = H2TestStore.CreateConnectionString(Name);
    public static new H2NorthwindTestStoreFactory Instance { get; } = new();

    // Bulk updates pass a transaction between contexts of one class, so they need the store's
    // shared connection; their private store name keeps that transaction away from other classes.
    public static H2NorthwindTestStoreFactory BulkUpdates { get; } = new(useConnectionString: false);

    protected H2NorthwindTestStoreFactory(bool useConnectionString = true)
        // Many Northwind fixtures share this store while xunit runs their classes in parallel.
        // A single store connection would be closed by one fixture's teardown while another is
        // still querying; H2Connection.Close() cancels its session and produces 57014.
        // The converted dump supplies the full schema and data; H2TestStore skips EF seeding
        // when a script is present, so the rows are loaded exactly once.
        : base(scriptPath: "NorthwindH2.sql", useConnectionString: useConnectionString)
    {
    }

    public override TestStore GetOrCreate(string storeName)
        // The script seeds every store, so a fixture that asks for its own database still gets the
        // full Northwind data without sharing another fixture's transaction.
        => base.GetOrCreate(storeName);
}
