using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

#pragma warning disable CS0618 // H2TsVector.Parse is obsolete

public class FullTextSearchDbFunctionsH2Test : IClassFixture<NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    protected NorthwindQueryH2Fixture<NoopModelCustomizer> Fixture { get; }

    // ReSharper disable once UnusedParameter.Local
    public FullTextSearchDbFunctionsH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }






























































}
