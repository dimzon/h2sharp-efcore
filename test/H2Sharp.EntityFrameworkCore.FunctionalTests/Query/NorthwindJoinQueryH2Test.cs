namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindJoinQueryH2Test : NorthwindJoinQueryRelationalTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindJoinQueryH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        ClearLog();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // #2759
    public override Task Join_local_collection_int_closure_is_cached_correctly(bool async)
        => base.Join_local_collection_int_closure_is_cached_correctly(async);
    // => Assert.ThrowsAsync<InvalidOperationException>(() => base.Join_local_collection_int_closure_is_cached_correctly(async));

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();
}
