namespace H2Sharp.EntityFrameworkCore.Query;

// H2 2.5 has no LATERAL and does not resolve a qualified outer column from inside a DISTINCT
// projection in a correlated subquery. Those shapes are skipped with the reference-jar reason;
// the two decimal-division tests are a real ADO/port defect and stay under BlockedByPort.
[Collection("Northwind")]
public class NorthwindSelectQueryH2Test : NorthwindSelectQueryRelationalTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    private const string NoLateral = "H2 2.5 has no LATERAL; verified on h2.jar";
    private const string NoQualifiedOuterColumn =
        "H2 2.5 does not resolve a qualified outer column in this correlated DISTINCT shape; verified on h2.jar";

    // ReSharper disable once UnusedParameter.Local
    public NorthwindSelectQueryH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        ClearLog();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    public override async Task Select_datetime_DayOfWeek_component(bool async)
    {
        await base.Select_datetime_DayOfWeek_component(async);

        AssertSql(
            """
SELECT mod(iso_day_of_week("o"."OrderDate"), 7)
FROM "Orders" "o"
""");
    }

    public override async Task Correlated_collection_after_distinct_with_complex_projection_not_containing_original_identifier(bool async)
    {
        // Identifier set for Distinct. Issue #24440.
        Assert.Equal(
            RelationalStrings.InsufficientInformationToIdentifyElementOfCollectionJoin,
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Correlated_collection_after_distinct_with_complex_projection_not_containing_original_identifier(async)))
            .Message);

        AssertSql();
    }

    public override async Task
        SelectMany_with_collection_being_correlated_subquery_which_references_non_mapped_properties_from_inner_and_outer_entity(
            bool async)
    {
        await AssertUnableToTranslateEFProperty(
            () => base
                .SelectMany_with_collection_being_correlated_subquery_which_references_non_mapped_properties_from_inner_and_outer_entity(
                    async));

        AssertSql();
    }

    public override Task Member_binding_after_ctor_arguments_fails_with_client_eval(bool async)
        => AssertTranslationFailed(() => base.Member_binding_after_ctor_arguments_fails_with_client_eval(async));

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Collection_projection_selecting_outer_element_followed_by_take(bool async)
        => base.Collection_projection_selecting_outer_element_followed_by_take(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Correlated_collection_after_distinct_not_containing_original_identifier(bool async)
        => base.Correlated_collection_after_distinct_not_containing_original_identifier(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Correlated_collection_after_distinct_with_complex_projection_containing_original_identifier(bool async)
        => base.Correlated_collection_after_distinct_with_complex_projection_containing_original_identifier(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Correlated_collection_after_groupby_with_complex_projection_containing_original_identifier(bool async)
        => base.Correlated_collection_after_groupby_with_complex_projection_containing_original_identifier(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Correlated_collection_after_groupby_with_complex_projection_not_containing_original_identifier(bool async)
        => base.Correlated_collection_after_groupby_with_complex_projection_not_containing_original_identifier(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault_2(bool async)
        => base.Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault_2(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Projecting_after_navigation_and_distinct(bool async)
        => base.Projecting_after_navigation_and_distinct(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Reverse_in_SelectMany_with_Take(bool async)
        => base.Reverse_in_SelectMany_with_Take(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Reverse_in_projection_subquery(bool async)
        => base.Reverse_in_projection_subquery(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Reverse_in_projection_subquery_single_result(bool async)
        => base.Reverse_in_projection_subquery_single_result(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_correlated_with_outer_1(bool async)
        => base.SelectMany_correlated_with_outer_1(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_correlated_with_outer_2(bool async)
        => base.SelectMany_correlated_with_outer_2(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_correlated_with_outer_3(bool async)
        => base.SelectMany_correlated_with_outer_3(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_correlated_with_outer_4(bool async)
        => base.SelectMany_correlated_with_outer_4(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_correlated_with_outer_5(bool async)
        => base.SelectMany_correlated_with_outer_5(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_correlated_with_outer_6(bool async)
        => base.SelectMany_correlated_with_outer_6(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_correlated_with_outer_7(bool async)
        => base.SelectMany_correlated_with_outer_7(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_whose_selector_references_outer_source(bool async)
        => base.SelectMany_whose_selector_references_outer_source(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task SelectMany_with_collection_being_correlated_subquery_which_references_inner_and_outer_entity(bool async)
        => base.SelectMany_with_collection_being_correlated_subquery_which_references_inner_and_outer_entity(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Select_nested_collection_deep(bool async)
        => base.Select_nested_collection_deep(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Select_nested_collection_deep_distinct_no_identifiers(bool async)
        => base.Select_nested_collection_deep_distinct_no_identifiers(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Set_operation_in_pending_collection(bool async)
        => base.Set_operation_in_pending_collection(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Take_on_correlated_collection_in_first(bool async)
        => base.Take_on_correlated_collection_in_first(async);

    [ConditionalTheory(Skip = NoLateral)]
    public override Task Take_on_top_level_and_on_collection_projection_with_outer_apply(bool async)
        => base.Take_on_top_level_and_on_collection_projection_with_outer_apply(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_OrderBy_Distinct_and_FirstOrDefault_followed_by_projecting_length(bool async)
        => base.Project_single_element_from_collection_with_OrderBy_Distinct_and_FirstOrDefault_followed_by_projecting_length(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault(bool async)
        => base.Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault_with_parameter(bool async)
        => base.Project_single_element_from_collection_with_OrderBy_Take_and_FirstOrDefault_with_parameter(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_OrderBy_Take_and_SingleOrDefault(bool async)
        => base.Project_single_element_from_collection_with_OrderBy_Take_and_SingleOrDefault(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault(bool async)
        => base.Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_2(bool async)
        => base.Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_2(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_followed_by_projection_of_length_property(bool async)
        => base.Project_single_element_from_collection_with_multiple_OrderBys_Take_and_FirstOrDefault_followed_by_projection_of_length_property(async);

    [ConditionalTheory(Skip = NoQualifiedOuterColumn)]
    public override Task Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault(bool async)
        => base.Project_single_element_from_collection_with_OrderBy_over_navigation_Take_and_FirstOrDefault(async);

    // H2 divides DECIMAL to scale 36. The provider rounds decimal division in SQL to
    // scale 28, so the ADO boundary can keep its exact conversion rule.
    public override Task Explicit_cast_in_arithmetic_operation_is_preserved(bool async)
        => base.Explicit_cast_in_arithmetic_operation_is_preserved(async);

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();
}
