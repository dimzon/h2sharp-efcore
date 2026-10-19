namespace H2Sharp.EntityFrameworkCore.Query;

public class IncludeH2Fixture : NorthwindQueryH2Fixture<NoopModelCustomizer>
{
    protected override bool ShouldLogCategory(string logCategory)
        => base.ShouldLogCategory(logCategory) || logCategory == DbLoggerCategory.Query.Name;
}
