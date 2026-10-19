namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class H2NorthwindTestStoreFactory : H2TestStoreFactory
{
    public const string Name = "Northwind";
    public static readonly string NorthwindConnectionString = H2TestStore.CreateConnectionString(Name);
    public static new H2NorthwindTestStoreFactory Instance { get; } = new();

    static H2NorthwindTestStoreFactory()
    {
        // TODO: Switch to using H2DataSource
#pragma warning disable CS0618 // Type or member is obsolete
        H2Connection.GlobalTypeMapper.EnableDynamicJson();
        H2Connection.GlobalTypeMapper.EnableRecordsAsTuples();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    protected H2NorthwindTestStoreFactory()
    {
    }

    public override TestStore GetOrCreate(string storeName)
        => H2TestStore.GetOrCreate(
            Name,
            scriptPath: "Northwind.sql",
            additionalSql: TestEnvironment.PostgresVersion >= new Version(12, 0)
                ? """CREATE COLLATION IF NOT EXISTS "some-case-insensitive-collation" (LOCALE = 'en-u-ks-primary', PROVIDER = icu, DETERMINISTIC = False);"""
                : null);
}
