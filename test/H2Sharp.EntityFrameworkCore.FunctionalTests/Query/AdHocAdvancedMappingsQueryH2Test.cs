using H2Sharp.EntityFrameworkCore.TestUtilities;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class AdHocAdvancedMappingsQueryH2Test : AdHocAdvancedMappingsQueryRelationalTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;

    // The PG guards below expect exceptions this provider does not produce.
    [Trait("Category", "BlockedByPort")]
    public override Task Query_generates_correct_datetime2_parameter_definition(int? fractionalSeconds, string postfix)
        => Assert.ThrowsAsync<ArgumentException>(
            () => base.Query_generates_correct_datetime2_parameter_definition(fractionalSeconds, postfix));

    [Trait("Category", "BlockedByPort")]
    public override Task Query_generates_correct_datetimeoffset_parameter_definition(int? fractionalSeconds, string postfix)
        => Assert.ThrowsAsync<ArgumentException>(
            () => base.Query_generates_correct_datetime2_parameter_definition(fractionalSeconds, postfix));

    [Trait("Category", "BlockedByPort")]
    public override Task Projecting_one_of_two_similar_complex_types_picks_the_correct_one()
        => Assert.ThrowsAsync<DbUpdateException>(
            () => base.Projecting_one_of_two_similar_complex_types_picks_the_correct_one());

    [Trait("Category", "BlockedByPort")]
    public override Task Projecting_expression_with_converter_with_closure(bool async)
        => Assert.ThrowsAsync<DbUpdateException>(
            () => base.Projecting_one_of_two_similar_complex_types_picks_the_correct_one());

    [Trait("Category", "BlockedByPort")]
    public override Task Projecting_property_with_converter_with_closure(bool async)
        => Assert.ThrowsAsync<DbUpdateException>(
            () => base.Projecting_one_of_two_similar_complex_types_picks_the_correct_one());

    [Trait("Category", "BlockedByPort")]
    public override Task Projecting_property_with_converter_without_closure(bool async)
        => Assert.ThrowsAsync<DbUpdateException>(
            () => base.Projecting_one_of_two_similar_complex_types_picks_the_correct_one());
}
