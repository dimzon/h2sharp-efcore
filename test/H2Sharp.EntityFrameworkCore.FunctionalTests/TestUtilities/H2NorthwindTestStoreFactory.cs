namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class H2NorthwindTestStoreFactory : H2TestStoreFactory
{
    public const string Name = "Northwind";
    public static readonly string NorthwindConnectionString = H2TestStore.CreateConnectionString(Name);
    public static new H2NorthwindTestStoreFactory Instance { get; } = new();

    protected H2NorthwindTestStoreFactory()
        // Many Northwind fixtures share this store while xunit runs their classes in parallel.
        // A single store connection would be closed by one fixture's teardown while another is
        // still querying; H2Connection.Close() cancels its session and produces 57014.
        // The converted dump supplies the full schema and data; H2TestStore skips EF seeding
        // when a script is present, so the rows are loaded exactly once.
        : base(scriptPath: "NorthwindH2.sql", useConnectionString: true)
    {
    }

    public override TestStore GetOrCreate(string storeName)
        // All Northwind fixtures use the one database the converted dump seeds.
        => base.GetOrCreate(Name);
}
