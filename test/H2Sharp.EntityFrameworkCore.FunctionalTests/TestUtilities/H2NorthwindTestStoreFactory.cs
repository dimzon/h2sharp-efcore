namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class H2NorthwindTestStoreFactory : H2TestStoreFactory
{
    public const string Name = "Northwind";
    public static readonly string NorthwindConnectionString = H2TestStore.CreateConnectionString(Name);
    public static new H2NorthwindTestStoreFactory Instance { get; } = new();

    protected H2NorthwindTestStoreFactory()
    {
    }

    public override TestStore GetOrCreate(string storeName)
        // Northwind.sql is a PostgreSQL dump: SET statement_timeout, CREATE EXTENSION, SERIAL,
        // bytea and 3202 E'\\x...' literals. The reference engine refuses it. EF's Northwind
        // fixture already seeds NorthwindData after the store is initialized, so let H2 create
        // the schema from NorthwindH2Context and load the data through EF instead.
        => base.GetOrCreate(Name);
}
