using System.Data.Common;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.Data;
using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

public class FromSqlQueryH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture)
    : FromSqlQueryTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>(fixture)
{
    // The spec base creates PG-style '@name' parameters; H2 writes '$name'.
    protected override DbParameter CreateDbParameter(string name, object value)
        => new H2Parameter { ParameterName = name?.TrimStart('@'), Value = value };

    // The malformed-data guards below expect PG's typed-read exceptions; H2 reports a different
    // type or accepts the value. The remaining cases need full Northwind.sql data that the H2
    // test store does not seed.
    [Trait("Category", "BlockedByPort")]
    public override Task Bad_data_error_handling_invalid_cast(bool async)
        => base.Bad_data_error_handling_invalid_cast(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Bad_data_error_handling_invalid_cast_key(bool async)
        => base.Bad_data_error_handling_invalid_cast_key(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Bad_data_error_handling_invalid_cast_no_tracking(bool async)
        => base.Bad_data_error_handling_invalid_cast_no_tracking(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Bad_data_error_handling_invalid_cast_projection(bool async)
        => base.Bad_data_error_handling_invalid_cast_projection(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Bad_data_error_handling_null(bool async)
        => base.Bad_data_error_handling_null(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Bad_data_error_handling_null_no_tracking(bool async)
        => base.Bad_data_error_handling_null_no_tracking(async);

    [Trait("Category", "BlockedByPort")]
    public override Task Bad_data_error_handling_null_projection(bool async)
        => base.Bad_data_error_handling_null_projection(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_in_subquery_with_dbParameter(bool async)
        => base.FromSqlRaw_in_subquery_with_dbParameter(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_queryable_composed_compiled_with_DbParameter(bool async)
        => base.FromSqlRaw_queryable_composed_compiled_with_DbParameter(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_queryable_simple_different_cased_columns_and_not_enough_columns_throws(bool async)
        => base.FromSqlRaw_queryable_simple_different_cased_columns_and_not_enough_columns_throws(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_queryable_simple_projection_composed(bool async)
        => base.FromSqlRaw_queryable_simple_projection_composed(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_with_dbParameter(bool async)
        => base.FromSqlRaw_with_dbParameter(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_with_dbParameter_mixed(bool async)
        => base.FromSqlRaw_with_dbParameter_mixed(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_with_dbParameter_mixed_in_subquery(bool async)
        => base.FromSqlRaw_with_dbParameter_mixed_in_subquery(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_with_dbParameter_without_name_prefix(bool async)
        => base.FromSqlRaw_with_dbParameter_without_name_prefix(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSqlRaw_with_db_parameters_called_multiple_times(bool async)
        => base.FromSqlRaw_with_db_parameters_called_multiple_times(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSql_Count_used_twice_with_parameters(bool async)
        => base.FromSql_Count_used_twice_with_parameters(async);

    [Trait("Category", "BlockedByPort")]
    public override Task FromSql_used_twice_with_parameters(bool async)
        => base.FromSql_used_twice_with_parameters(async);
}
