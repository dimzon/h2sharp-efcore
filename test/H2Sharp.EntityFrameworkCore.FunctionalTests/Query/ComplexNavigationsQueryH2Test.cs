using Xunit.Sdk;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class ComplexNavigationsQueryH2Test : ComplexNavigationsQueryRelationalTestBase<ComplexNavigationsQueryH2Fixture>
{
    public ComplexNavigationsQueryH2Test(ComplexNavigationsQueryH2Fixture fixture)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public override async Task Join_with_result_selector_returning_queryable_throws_validation_error(bool async)
        => await Assert.ThrowsAsync<ArgumentException>(
            () => base.Join_with_result_selector_returning_queryable_throws_validation_error(async));

    public override Task GroupJoin_client_method_in_OrderBy(bool async)
        => AssertTranslationFailedWithDetails(
            () => base.GroupJoin_client_method_in_OrderBy(async),
            CoreStrings.QueryUnableToTranslateMethod(
                "Microsoft.EntityFrameworkCore.Query.ComplexNavigationsQueryTestBase<H2Sharp.EntityFrameworkCore.Query.ComplexNavigationsQueryH2Fixture>",
                "ClientMethodNullableInt"));

    [Trait("Category", "BlockedByPort")]
    public override Task Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(bool async)
        => Assert.ThrowsAsync<EqualException>(
            async () => await base.Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(async));

    // H2 2.5 has no LATERAL for these correlated shapes.
    [Trait("Category", "BlockedByPort")]
    public override Task Contains_with_subquery_optional_navigation_and_constant_item(bool async)
        => base.Contains_with_subquery_optional_navigation_and_constant_item(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Correlated_projection_with_first(bool async)
        => base.Correlated_projection_with_first(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Let_let_contains_from_outer_let(bool async)
        => base.Let_let_contains_from_outer_let(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Multiple_select_many_in_projection(bool async)
        => base.Multiple_select_many_in_projection(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Prune_does_not_throw_null_ref(bool async)
        => base.Prune_does_not_throw_null_ref(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Single_select_many_in_projection_with_take(bool async)
        => base.Single_select_many_in_projection_with_take(async);
}
