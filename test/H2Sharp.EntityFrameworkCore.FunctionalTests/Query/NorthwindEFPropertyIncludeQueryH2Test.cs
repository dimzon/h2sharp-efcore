namespace H2Sharp.EntityFrameworkCore.Query;

public class NorthwindEFPropertyIncludeQueryH2Test : NorthwindEFPropertyIncludeQueryTestBase<
    NorthwindQueryH2Fixture<NoopModelCustomizer>>
{
    // ReSharper disable once UnusedParameter.Local
    public NorthwindEFPropertyIncludeQueryH2Test(NorthwindQueryH2Fixture<NoopModelCustomizer> fixture)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public override async Task Include_collection_with_last_no_orderby(bool async)
    {
        Assert.Equal(
            RelationalStrings.LastUsedWithoutOrderBy(nameof(Enumerable.Last)),
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => base.Include_collection_with_last_no_orderby(async))).Message);

        AssertSql();
    }

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
