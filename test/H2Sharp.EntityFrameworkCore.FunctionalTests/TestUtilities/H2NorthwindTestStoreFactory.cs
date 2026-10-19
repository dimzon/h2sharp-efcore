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
        // H2 has no ICU collation and no user-defined collations in the port yet, so the
        // case-insensitive collation the Northwind script asks for cannot be created. The
        // script that creates the Northwind data is H2's own, not PostgreSQL's.
        => H2TestStore.GetOrCreate(
            Name,
            scriptPath: "Northwind.sql");
}
