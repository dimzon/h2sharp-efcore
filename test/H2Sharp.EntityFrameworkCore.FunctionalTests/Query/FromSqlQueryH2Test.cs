using System.Data.Common;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.Data;
using Xunit;

namespace H2Sharp.EntityFrameworkCore.Query;

[Trait("Category", "BlockedByPort")]
public class FromSqlQueryH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture)
    : FromSqlQueryTestBase<NorthwindQueryH2Fixture<NoopModelCustomizer>>(fixture)
{
    protected override DbParameter CreateDbParameter(string name, object value)
        => new H2Parameter { ParameterName = name, Value = value };
}
