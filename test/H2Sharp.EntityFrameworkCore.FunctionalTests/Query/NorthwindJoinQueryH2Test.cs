namespace H2Sharp.EntityFrameworkCore.Query;

[Collection("Northwind")]
public class NorthwindJoinQueryH2Test : NorthwindJoinQueryRelationalTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindJoinQueryH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        ClearLog();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // H2 2.5 has no LATERAL: EF's translated CROSS/OUTER APPLY cannot be written as SQL.
    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task GroupJoin_on_true_equal_true(bool async)
        => base.GroupJoin_on_true_equal_true(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task SelectMany_with_client_eval(bool async)
        => base.SelectMany_with_client_eval(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task SelectMany_with_client_eval_with_collection_shaper(bool async)
        => base.SelectMany_with_client_eval_with_collection_shaper(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task SelectMany_with_client_eval_with_collection_shaper_ignored(bool async)
        => base.SelectMany_with_client_eval_with_collection_shaper_ignored(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task SelectMany_with_selecting_outer_element(bool async)
        => base.SelectMany_with_selecting_outer_element(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task SelectMany_with_selecting_outer_entity(bool async)
        => base.SelectMany_with_selecting_outer_entity(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task SelectMany_with_selecting_outer_entity_column_and_inner_column(bool async)
        => base.SelectMany_with_selecting_outer_entity_column_and_inner_column(async);

    [ConditionalTheory(Skip = "H2 2.5 has no LATERAL, so an apply cannot be written as SQL")]
    public override Task Take_in_collection_projection_with_FirstOrDefault_on_top_level(bool async)
        => base.Take_in_collection_projection_with_FirstOrDefault_on_top_level(async);

    // #2759
    public override Task Join_local_collection_int_closure_is_cached_correctly(bool async)
        => base.Join_local_collection_int_closure_is_cached_correctly(async);
    // => Assert.ThrowsAsync<InvalidOperationException>(() => base.Join_local_collection_int_closure_is_cached_correctly(async));

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();
}
