using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class QueryFilterFuncletizationH2Test
    : QueryFilterFuncletizationTestBase<QueryFilterFuncletizationH2Test.QueryFilterFuncletizationH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public QueryFilterFuncletizationH2Test(
        QueryFilterFuncletizationH2Fixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public override void DbContext_list_is_parameterized()
    {
        using var context = CreateContext();

        // The standard EF Core implementation expands the query filter-referenced list client side, and so generates an NRE
        // when the list is null. We translate to server-side with H2AnyExpression, so no exception is thrown.
        // Assert.Throws<NullReferenceException>(() => context.Set<ListFilter>().ToList());

        context.TenantIds = [];
        var query = context.Set<ListFilter>().ToList();
        Assert.Empty(query);

        context.TenantIds = [1];
        query = context.Set<ListFilter>().ToList();
        Assert.Single(query);

        context.TenantIds = [2, 3];
        query = context.Set<ListFilter>().ToList();
        Assert.Equal(2, query.Count);
    }

    public class QueryFilterFuncletizationH2Fixture : QueryFilterFuncletizationRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;
    }
}
