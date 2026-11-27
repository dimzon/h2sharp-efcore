using H2Sharp.EntityFrameworkCore.TestUtilities;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class AdHocNavigationsQueryH2Test : AdHocNavigationsQueryRelationalTestBase
{
    // H2 accepts the DateTime this PG-specific guard expected to reject.
    public override Task Reference_include_on_derived_type_with_sibling_works()
        => base.Reference_include_on_derived_type_with_sibling_works();

    // H2 2.5 has no LATERAL, so these outer-apply shapes cannot be written as SQL.
    [ConditionalFact]
    [Trait("Category", "BlockedByPort")]
    public override Task Let_multiple_references_with_reference_to_outer()
        => base.Let_multiple_references_with_reference_to_outer();

    [ConditionalFact]
    [Trait("Category", "BlockedByPort")]
    public override Task SelectMany_and_collection_in_projection_in_FirstOrDefault()
        => base.SelectMany_and_collection_in_projection_in_FirstOrDefault();

    [ConditionalFact]
    [Trait("Category", "BlockedByPort")]
    public override Task Projection_with_multiple_includes_and_subquery_with_set_operation()
        => base.Projection_with_multiple_includes_and_subquery_with_set_operation();

    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;
}
