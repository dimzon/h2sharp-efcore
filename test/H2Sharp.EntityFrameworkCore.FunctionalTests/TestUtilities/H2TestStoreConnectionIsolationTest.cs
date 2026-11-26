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
    }

    private static void AssertDistinctConnections(TestStore store)
    {
        using var first = new DbContext(store.AddProviderOptions(new DbContextOptionsBuilder()).Options);
        using var second = new DbContext(store.AddProviderOptions(new DbContextOptionsBuilder()).Options);

        Assert.NotSame(first.Database.GetDbConnection(), second.Database.GetDbConnection());
    }
}
