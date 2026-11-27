using Microsoft.EntityFrameworkCore.TestModels.Northwind;

using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindAggregateOperatorsQueryH2Test : NorthwindAggregateOperatorsQueryRelationalTestBase<
    NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindAggregateOperatorsQueryH2Test(
        NorthwindQueryH2Fixture<NoopModelCustomizer> fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        ClearLog();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    // H2's decimal averaging differs from the PG result this test asserts.
    [Trait("Category", "BlockedByPort")]
    public override async Task Average_over_max_subquery(bool async)
    {
        await AssertAverage(
            async,
            ss => ss.Set<Customer>().OrderBy(c => c.CustomerID).Take(3),
            selector: c => (decimal)c.Orders.Average(o => 5 + o.OrderDetails.Max(od => od.ProductID)),
            asserter: (e, a) => Assert.Equal(e, a, 10));

        AssertSql(
            """
$__p_0='3'

SELECT avg((
    SELECT avg(CAST(5 + (
        SELECT max(o0."ProductID")
        FROM "Order Details" AS o0
        WHERE o."OrderID" = o0."OrderID") AS double precision))
    FROM "Orders" AS o
    WHERE c0."CustomerID" = o."CustomerID")::numeric)
FROM (
    SELECT "c"."CustomerID"
    FROM "Customers" "c"
    ORDER BY "c"."CustomerID" NULLS FIRST
    LIMIT $__p_0
) AS c0
""");
    }

    public override async Task Contains_with_local_uint_array_closure(bool async)
    {
        await base.Contains_with_local_uint_array_closure(async);

        // Note: PostgreSQL doesn't support uint, but value converters make this into bigint
        AssertSql(
            """
$__ids_0={ '0', '1' } (DbType = Int32)

SELECT "e"."EmployeeID", "e"."City", "e"."Country", "e"."FirstName", "e"."ReportsTo", "e"."Title"
FROM "Employees" "e"
WHERE "e"."EmployeeID" = ANY ($__ids_0)
""",
            //
            """
$__ids_0={ '0' } (DbType = Int32)

SELECT "e"."EmployeeID", "e"."City", "e"."Country", "e"."FirstName", "e"."ReportsTo", "e"."Title"
FROM "Employees" "e"
WHERE "e"."EmployeeID" = ANY ($__ids_0)
""");
    }

    public override async Task Contains_with_local_nullable_uint_array_closure(bool async)
    {
        await base.Contains_with_local_nullable_uint_array_closure(async);

        // Note: PostgreSQL doesn't support uint, but value converters make this into bigint

        AssertSql(
            """
$__ids_0={ '0', '1' } (DbType = Int32)

SELECT "e"."EmployeeID", "e"."City", "e"."Country", "e"."FirstName", "e"."ReportsTo", "e"."Title"
FROM "Employees" "e"
WHERE "e"."EmployeeID" = ANY ($__ids_0)
""",
            //
            """
$__ids_0={ '0' } (DbType = Int32)

SELECT "e"."EmployeeID", "e"."City", "e"."Country", "e"."FirstName", "e"."ReportsTo", "e"."Title"
FROM "Employees" "e"
WHERE "e"."EmployeeID" = ANY ($__ids_0)
""");
    }

    public override Task Contains_with_local_anonymous_type_array_closure(bool async)
        // Aggregates. Issue #15937.
        => AssertTranslationFailed(() => base.Contains_with_local_anonymous_type_array_closure(async));

    [Trait("Category", "BlockedByPort")]
    public override Task Contains_with_local_tuple_array_closure(bool async)
        => Assert.ThrowsAsync<InvalidCastException>(() => base.Contains_with_local_tuple_array_closure(async: true));

    public override async Task Contains_with_local_enumerable_inline(bool async)
    {
        // Issue #31776
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await base.Contains_with_local_enumerable_inline(async));

        AssertSql();
    }

    // H2 2.5 has no ARRAY_REMOVE function.
    [Trait("Category", "BlockedByPort")]
    public override async Task Contains_with_local_enumerable_inline_closure_mix(bool async)
    {
        await base.Contains_with_local_enumerable_inline_closure_mix(async);

        AssertSql(
            """
$__p_0={ 'ABCDE', 'ALFKI' } (DbType = String)

SELECT "c"."CustomerID", "c"."Address", "c"."City", "c"."CompanyName", "c"."ContactName", "c"."ContactTitle", "c"."Country", "c"."Fax", "c"."Phone", "c"."PostalCode", "c"."Region"
FROM "Customers" "c"
WHERE "c"."CustomerID" = ANY (array_remove($__p_0, NULL))
""",
            //
            """
$__p_0={ 'ABCDE', 'ANATR' } (DbType = String)

SELECT "c"."CustomerID", "c"."Address", "c"."City", "c"."CompanyName", "c"."ContactName", "c"."ContactTitle", "c"."Country", "c"."Fax", "c"."Phone", "c"."PostalCode", "c"."Region"
FROM "Customers" "c"
WHERE "c"."CustomerID" = ANY (array_remove($__p_0, NULL))
""");
    }

    public override async Task Contains_with_local_non_primitive_list_closure_mix(bool async)
    {
        await base.Contains_with_local_non_primitive_list_closure_mix(async);

        AssertSql(
            """
$__Select_0={ 'ABCDE', 'ALFKI' } (DbType = String)

SELECT "c"."CustomerID", "c"."Address", "c"."City", "c"."CompanyName", "c"."ContactName", "c"."ContactTitle", "c"."Country", "c"."Fax", "c"."Phone", "c"."PostalCode", "c"."Region"
FROM "Customers" "c"
WHERE "c"."CustomerID" = ANY ($__Select_0)
""");
    }

    public override async Task Contains_with_local_non_primitive_list_inline_closure_mix(bool async)
    {
        await base.Contains_with_local_non_primitive_list_inline_closure_mix(async);

        AssertSql(
            """
$__Select_0={ 'ABCDE', 'ALFKI' } (DbType = String)

SELECT "c"."CustomerID", "c"."Address", "c"."City", "c"."CompanyName", "c"."ContactName", "c"."ContactTitle", "c"."Country", "c"."Fax", "c"."Phone", "c"."PostalCode", "c"."Region"
FROM "Customers" "c"
WHERE "c"."CustomerID" = ANY ($__Select_0)
""",
            //
            """
$__Select_0={ 'ABCDE', 'ANATR' } (DbType = String)

SELECT "c"."CustomerID", "c"."Address", "c"."City", "c"."CompanyName", "c"."ContactName", "c"."ContactTitle", "c"."Country", "c"."Fax", "c"."Phone", "c"."PostalCode", "c"."Region"
FROM "Customers" "c"
WHERE "c"."CustomerID" = ANY ($__Select_0)
""");
    }

    [Trait("Category", "BlockedByPort")]
    public override Task Average_over_nested_subquery(bool async)
        => base.Average_over_nested_subquery(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery_complex(bool async)
        => base.Contains_over_entityType_with_null_should_rewrite_to_identity_equality_subquery_complex(async);

    // H2 2.5 has no LATERAL for this outer-apply shape.
    [Trait("Category", "BlockedByPort")]
    public override Task Multiple_collection_navigation_with_FirstOrDefault_chained(bool async)
        => base.Multiple_collection_navigation_with_FirstOrDefault_chained(async);

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    protected override void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();
}
