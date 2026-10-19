using Xunit.Sdk;

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

    public override Task Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(bool async)
        => Assert.ThrowsAsync<EqualException>(
            async () => await base.Nested_SelectMany_correlated_with_join_table_correctly_translated_to_apply(async));
}
