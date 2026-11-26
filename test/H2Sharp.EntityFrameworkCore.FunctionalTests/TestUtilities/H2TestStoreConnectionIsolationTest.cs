using H2Sharp.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace H2Sharp.EntityFrameworkCore;

public class H2TestStoreConnectionIsolationTest
{
    [Fact]
    public void Parallel_query_fixtures_do_not_share_one_connection()
    {
        var northwind = H2NorthwindTestStoreFactory.Instance.GetOrCreate(H2NorthwindTestStoreFactory.Name);
        AssertDistinctConnections(northwind);

        var manyToMany = H2TestStoreFactory.ManyToMany.GetOrCreate("ConnectionIsolation");
        AssertDistinctConnections(manyToMany);

        // The concurrency fixtures hold transactions on their store connections, so they must not
        // share one with each other or with the default store used by the other F1 fixtures.
        var concurrency = H2TestStoreFactory.Concurrency.GetOrCreate("F1ConcurrencyTest");
        var bytes = H2TestStoreFactory.ConcurrencyBytes.GetOrCreate("F1BytesConcurrencyTest");
        var defaultStore = H2TestStoreFactory.Instance.GetOrCreate("ConnectionIsolationDefault");
        using var first = new DbContext(concurrency.AddProviderOptions(new DbContextOptionsBuilder()).Options);
        using var second = new DbContext(bytes.AddProviderOptions(new DbContextOptionsBuilder()).Options);
        using var third = new DbContext(defaultStore.AddProviderOptions(new DbContextOptionsBuilder()).Options);
        Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
        Assert.NotSame(first.Database.GetDbConnection(), third.Database.GetDbConnection());
        Assert.NotSame(second.Database.GetDbConnection(), third.Database.GetDbConnection());
    }

    private static void AssertDistinctConnections(TestStore store)
    {
        using var first = new DbContext(store.AddProviderOptions(new DbContextOptionsBuilder()).Options);
        using var second = new DbContext(store.AddProviderOptions(new DbContextOptionsBuilder()).Options);

        Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
    }
}
